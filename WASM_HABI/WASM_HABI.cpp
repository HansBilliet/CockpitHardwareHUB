// WASM_HABI.cpp

#include <stdio.h>
#include <MSFS/MSFS.h>
#include <MSFS/MSFS_WindowsTypes.h>
#include <SimConnect.h>
#include <MSFS/Legacy/gauges.h>

#include <vector>
#include <chrono>

#include "WASM_HABI.h"

using namespace std;

const char* WASM_Name = "HABI_WASM";
const char* WASM_Version = "00.01";

const char* CLIENT_DATA_NAME_COMMAND = "HW.Command";
const SIMCONNECT_CLIENT_DATA_ID CLIENT_DATA_ID_COMMAND = 0;

const char* CLIENT_DATA_NAME_ACKNOWLEDGE = "HW.Acknowledge";
const SIMCONNECT_CLIENT_DATA_ID CLIENT_DATA_ID_ACKNOWLEDGE = 1;

const char* CLIENT_DATA_NAME_RESULT = "HW.Result";
const SIMCONNECT_CLIENT_DATA_ID CLIENT_DATA_ID_RESULT = 2;

const char* CLIENT_DATA_NAME_VARS = "HW.Vars_%i";
const SIMCONNECT_CLIENT_DATA_ID CLIENT_DATA_ID_VARS_START = 4;

const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DATA_DEFINITION_ID_STRING_COMMAND = 0;
const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DATA_DEFINITION_ID_ACKNOWLEDGE = 1;
const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DATA_DEFINITION_ID_RESULT = 2;
const UINT16 START_LVAR_DEFINITION = 10;

const int MESSAGE_SIZE = 256;
const int EVENT_NAME_SIZE = 32;
const UINT16 SIZE_CLIENTAREA_VARS = SIMCONNECT_CLIENTDATA_MAX_SIZE;

HANDLE g_hSimConnect;
UINT16 g_Offset = 0;
int g_Bank = -1;

#pragma pack(push, 1) // packing is now 1
struct VarData {
	char VarType;
	UINT16 lvID;
	UINT16 DefineID;
	UINT16 Bank;
	UINT16 Offset;
	char Name[MESSAGE_SIZE];
	UINT16 EventID;
	char ValType;
	char ValAccess;
	FLOAT64 f64;
	INT32 i32;
	char s256[256];
};
#pragma pack(pop) // packing is 8 again

vector<VarData> Vars;

#pragma pack(push, 1) // packing is now 1
struct Result {
	FLOAT64 exeF;
	SINT32 exeI;
	char exeS[256];
};
#pragma pack(pop) // packing is 8 again

enum eEvents
{
	EVENT_FLIGHT_LOADED,
	EVENT_FRAME,
	EVENT_TEST,
	EVENT_CUSTOM_START,
	//EVENT_1SEC,
	//EVENT_FLIGHTLOADED,
	//EVENT_AIRCRAFTLOADED
};

enum eRequestID
{
	CMD
};

enum HABI_WASM_GROUP
{
	GROUP
};

//uint64_t millis()
//{
//	uint64_t ms = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::
//		now().time_since_epoch()).count();
//	return ms;
//}
//
//// Get time stamp in microseconds.
//uint64_t micros()
//{
//	uint64_t us = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::high_resolution_clock::
//		now().time_since_epoch()).count();
//	return us;
//}
//
//// Get time stamp in nanoseconds.
//uint64_t nanos()
//{
//	uint64_t ns = std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::high_resolution_clock::
//		now().time_since_epoch()).count();
//	return ns;
//}

void ExecuteCalculatorCode(char* sExe)
{
	Result exeRes;
	exeRes.exeS[0] = '\0';
	PCSTRINGZ ps;

	execute_calculator_code(sExe, &exeRes.exeF, &exeRes.exeI, &ps);
	strncat(exeRes.exeS, ps, 255);

	fprintf(stderr, "%s: ExecuteCalulculatorCode: float %f, int %i, string %s", WASM_Name, exeRes.exeF, exeRes.exeI, exeRes.exeS);

	HRESULT hr = SimConnect_SetClientData(
		g_hSimConnect,
		CLIENT_DATA_ID_RESULT,
		DATA_DEFINITION_ID_RESULT,
		SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
		0,
		sizeof(exeRes),
		&exeRes
	);

	if (hr != S_OK)
	{
		fprintf(stderr, "%s: Error on Setting Client Data RESULT", WASM_Name);
	}
}

bool GetVarVal(VarData* &pVar)
{
	// What needs to happen
	// LVars can be FLOAT64 or VOID
	// XVars can be INT32, FLOAT64, STRING256 or VOID

	FLOAT64 f64;
	INT32 i32;
	PCSTRINGZ ps;

	if (pVar->ValAccess == 'W')
		return false;

	if (pVar->VarType == 'L')
	{
		switch (pVar->ValType)
		{
		case 'I':
			f64 = get_named_variable_value(pVar->lvID);
			if (f64 < INT32_MIN)
				i32 = INT32_MIN;
			else if (f64 > INT32_MAX)
				i32 = INT32_MAX;
			else
				i32 = static_cast<INT32>(f64);
			if (i32 == pVar->i32)
				return false; // no change in INT32 value
			else
				pVar->i32 = i32;
			break;
		case 'F':
			f64 = get_named_variable_value(pVar->lvID);
			if (f64 == pVar->f64)
				return false; // no change in FLOAT64 value
			else
				pVar->f64 = f64;
			break;
		default:
			return false;  // no return value needed
		}
	}
	else if (pVar->VarType == 'X')
	{
		switch (pVar->ValType)
		{
		case 'I':
			execute_calculator_code(pVar->Name, nullptr, &i32, nullptr);
			if (i32 == pVar->i32)
				return false; // no change in INT32 value
			else
				pVar->i32 = i32;
			break;
		case 'F':
			execute_calculator_code(pVar->Name, &f64, nullptr, nullptr);
			if (f64 == pVar->f64)
				return false; // no change in FLOAT64 value
			else
				pVar->f64 = f64;
			break;
		case 'S':
			execute_calculator_code(pVar->Name, nullptr, nullptr, &ps);
			if (strncmp(ps, pVar->s256, 255) == 0)
				return false; // no change in STRING256 value
			else
			{
				pVar->s256[0] = '\0';
				strncat(pVar->s256, ps, 255);
			}
			break;
		default:
			return false; // no return value needed
		}
	}
	else
		return false; // no correct VarType

	fprintf(stderr, "%s: GetVarVal change: f64=%f i32=%i s256=%s", WASM_Name, pVar->f64, pVar->i32, pVar->s256);
	return true;
}

UINT16 GetVarValSize(VarData* pVar)
{
	switch (pVar->ValType)
	{
	case 'I':
		return sizeof(INT32);
		break;
	case 'F':
		return sizeof(FLOAT64);
		break;
	case 'S':
		return 256;
		break;
	default:
		return 0;
		break;
	}
}

bool FindVar(char* sVar, char cVarType, char cValType, char cValAccess, VarData* &pVar)
{
	for (VarData &v : Vars)
	{
		if ((v.VarType == cVarType) && 
			(v.ValType == cValType) && 
			(v.ValAccess == cValAccess) && 
			(strncmp(v.Name, sVar, 255) == 0))
		{
			pVar = &v;
			fprintf(stderr, "%s: FindVar: %s - f64=%f i32=%i s256=%s addr=%p", WASM_Name, pVar->Name, pVar->f64, pVar->i32, pVar->s256, (void*)pVar);
			fprintf(stderr, "%s: FindVar: VarType: %c ValType: %c ValAccess: %c", WASM_Name, pVar->VarType, pVar->ValType, pVar->ValAccess);
			return true;
		}
	}

	return false;
}

void SetClientData(VarData* pVar)
{
	HRESULT hr;

	// Never set a value 'W'rite only
	if (pVar->ValAccess == 'W')
		return;

	switch (pVar->ValType)
	{
	case 'I':
		fprintf(stderr, "%s: Var %i", WASM_Name, pVar->i32);
		fprintf(stderr, "%s: Var %u %u", WASM_Name, pVar->Bank, pVar->DefineID);
		hr = SimConnect_SetClientData(
			g_hSimConnect,
			CLIENT_DATA_ID_VARS_START + pVar->Bank,
			pVar->DefineID,
			SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
			0,
			sizeof(pVar->i32),
			&pVar->i32
		);
		break;
	case 'F':
		fprintf(stderr, "%s: Var %f", WASM_Name, pVar->f64);
		fprintf(stderr, "%s: Var %u %u", WASM_Name, pVar->Bank, pVar->DefineID);
		hr = SimConnect_SetClientData(
			g_hSimConnect,
			CLIENT_DATA_ID_VARS_START + pVar->Bank,
			pVar->DefineID,
			SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
			0,
			sizeof(pVar->f64),
			&pVar->f64
		);
		break;
	case 'S':
		fprintf(stderr, "%s: Var %s", WASM_Name, pVar->s256);
		fprintf(stderr, "%s: Var %u %u", WASM_Name, pVar->Bank, pVar->DefineID);
		hr = SimConnect_SetClientData(
			g_hSimConnect,
			CLIENT_DATA_ID_VARS_START + pVar->Bank,
			pVar->DefineID,
			SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
			0,
			sizeof(pVar->s256),
			&pVar->s256
		);
		break;
	}

	if (hr != S_OK)
	{
		fprintf(stderr, "%s: Error on Setting Client Data VARS for variable %s.", WASM_Name, pVar->Name);
	}
}

void RegisterVar(char* sVar, char cVarType, char cValType, char cValAccess)
{
	VarData *pVar;
	HRESULT hr;

	// Search if Var is already registered
	if (!FindVar(sVar, cVarType, cValType, cValAccess, pVar))
	{
		// create a new variable
		VarData v;

		// initial the variable
		v.VarType = cVarType;
		v.ValType = cValType;
		v.ValAccess = cValAccess;
		v.f64 = 0;
		v.i32 = 0;
		v.s256[0] = '\0';
		v.Bank = -1;
		v.Offset = 0;

		if (cVarType == 'L')
		{
			// Check if LVar exists
			v.lvID = check_named_variable(sVar);

			// If LVAR does not exist, ignore
			if (v.lvID == -1)
			{
				fprintf(stderr, "%s: RegisterVar -> LVar \"%s\" does not exist", WASM_Name, sVar);
				return;
			}
		}

		// copy the name, limit at 256 characters (including '\0')
		v.Name[0] = '\0';
		strncat(v.Name, sVar, 255);
		fprintf(stderr, "%s: RegisterVar -> v.Name = %s", WASM_Name, v.Name);

		// Create new unique DefineID
		v.DefineID = Vars.size() + START_LVAR_DEFINITION;

		// Enable setting value through Event if requested
		if (v.ValAccess == 'W' || v.ValAccess == 'B' || v.ValType == 'V')
		{
			// Create Event to be able to set this LVAR
			// use Vars.size() + EVENT_CUSTOM_START as EventID, which will make it very easy to lookup
			char EventName[EVENT_NAME_SIZE];
			v.EventID = ((int)EVENT_CUSTOM_START + Vars.size());
			sprintf(EventName, "HW.EVENT_%04i", v.EventID); // Example: "HW.EVENT_1234"

			// Add LVar Event for setting data
			hr = SimConnect_MapClientEventToSimEvent(g_hSimConnect, v.EventID, EventName);
			if (hr != S_OK)
			{
				fprintf(stderr, "%s: SimConnect_MapClientEventToSimEvent failed.\n", WASM_Name);
				return;
			}

			hr = SimConnect_AddClientEventToNotificationGroup(g_hSimConnect, HABI_WASM_GROUP::GROUP, v.EventID, false);
			if (hr != S_OK)
			{
				fprintf(stderr, "%s: SimConnect_AddClientEventToNotificationGroup failed.\n", WASM_Name);
				return;
			}
		}

		// Enable polling if requested
		if ((v.ValAccess == 'R' || v.ValAccess == 'B') && v.ValType != 'V')
		{
			// Check if enough space in Client Area, if not, create another one
			UINT16 VarValSize = GetVarValSize(&v);
			if ((g_Bank == -1) || ((g_Offset + VarValSize) > SIZE_CLIENTAREA_VARS))
			{
				g_Offset = 0;	// Reset offset
				g_Bank++;		// Increase Bank nr

				// construct Client Area Data
				SIMCONNECT_CLIENT_DATA_ID ClientID = CLIENT_DATA_ID_VARS_START + g_Bank;
				char ClientDataNameVars[20];
				sprintf(ClientDataNameVars, CLIENT_DATA_NAME_VARS, g_Bank); // Example: "HW.Vars_0"

				hr = SimConnect_MapClientDataNameToID(g_hSimConnect, ClientDataNameVars, ClientID);
				if (hr != S_OK) {
					fprintf(stderr, "%s: MapClientDataNameToID failed: %s with ID %i. %u", WASM_Name, ClientDataNameVars, ClientID, hr);
					return;
				}

				hr = SimConnect_CreateClientData(g_hSimConnect, ClientID, SIZE_CLIENTAREA_VARS, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);
				if (hr != S_OK) {
					fprintf(stderr, "%s: CreateClientData failed: %s with ID %i. %u", WASM_Name, ClientDataNameVars, ClientID, hr);
					return;
				}
			}
			v.Offset = g_Offset;
			g_Offset += VarValSize;
			v.Bank = g_Bank;

			// Add Var in ClientDataDefinition for getting data
			hr = SimConnect_AddToClientDataDefinition(
				g_hSimConnect,
				v.DefineID,	// DefineID
				v.Offset,	// Offset
				VarValSize	// Size
			);
			fprintf(stderr, "%s: AddToClientDataDefinition: DefineID: %u Offset: %u VarValSize: %u", WASM_Name, v.DefineID, v.Offset, VarValSize);
		}

		// Add at the end of the variables and retrieve the pointer to that element
		Vars.push_back(v);
		pVar = &Vars.back();

		// Get current value of variable if Read access
		if (GetVarVal(pVar))
			SetClientData(pVar);
	}

	fprintf(stderr, "%s: RegisterVar: %s - f64=%f i32=%i s256=%s addr=%p", WASM_Name, pVar->Name, pVar->f64, pVar->i32, pVar->s256, (void*)pVar);
	fprintf(stderr, "%s: RegisterVar: VarType: %c ValType: %c ValAccess: %c", WASM_Name, pVar->VarType, pVar->ValType, pVar->ValAccess);

	// send acknowledge back to client
	hr = SimConnect_SetClientData(
		g_hSimConnect,
		CLIENT_DATA_ID_ACKNOWLEDGE,
		DATA_DEFINITION_ID_ACKNOWLEDGE,
		SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
		0,
		sizeof(VarData),
		pVar
	);

	if (hr != S_OK)
	{
		fprintf(stderr, "%s: Error on Setting Client Data ACK for variable %s.", WASM_Name, pVar->Name);
	}
}

void ReadVars()
{
	for (auto i = 0; i < Vars.size(); i++)
	{
		VarData* pVar = &Vars[i];
		if (GetVarVal(pVar))
		{
			fprintf(stderr, "%s: Var %s with ID %u changed", WASM_Name, pVar->Name, pVar->DefineID);
			SetClientData(pVar);
		}
	}
}

void CALLBACK MyDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
	switch (pData->dwID)
	{
	case SIMCONNECT_RECV_ID_EVENT:
	{
		SIMCONNECT_RECV_EVENT* evt = (SIMCONNECT_RECV_EVENT*)pData;

		if (evt->uEventID >= EVENT_CUSTOM_START)
		{
			fprintf(stderr, "%s: RECV_EVENT: Event %u received", WASM_Name, evt->uEventID);
			fprintf(stderr, "%s: RECV_EVENT: Vars[%u]", WASM_Name, evt->uEventID - EVENT_CUSTOM_START);
			VarData &v = Vars[evt->uEventID - EVENT_CUSTOM_START];

			if (v.VarType == 'L')
			{
				fprintf(stderr, "%s: RECV_EVENT - \"L\": set_named_variable_value(%i, %d)", WASM_Name, v.lvID, evt->dwData);
				set_named_variable_value(v.lvID, evt->dwData);
			}
			else if (v.VarType == 'X')
			{
				if (v.ValAccess != 'R') // [W]rite or [B]oth
				{
					// Provide buffer for DWORD (max "4294967295") and VarName
					char s[256 + 10];
					// Use the dwData as parameter
					sprintf(s, "%u %s", evt->dwData, &v.Name);

					fprintf(stderr, "%s: RECV_EVENT - \"X\": execute_calculator_code(%s)", WASM_Name, s);
					execute_calculator_code(s, nullptr, nullptr, nullptr);
				}
				else // [R]ead
				{
					fprintf(stderr, "%s: RECV_EVENT - \"X\": execute_calculator_code(%s)", WASM_Name, v.Name);
					execute_calculator_code(v.Name, nullptr, nullptr, nullptr);
				}
			}
		}

		break; // end case SIMCONNECT_RECV_ID_EVENT
	}

	case SIMCONNECT_RECV_ID_CLIENT_DATA:
	{
		SIMCONNECT_RECV_CLIENT_DATA* recv_data = (SIMCONNECT_RECV_CLIENT_DATA*)pData;

		if (recv_data->dwRequestID == CMD)
		{
			char* sCmd = (char*)&recv_data->dwData;
			fprintf(stderr, "%s: RECV_CLIENT_DATA: \"%s\"", WASM_Name, sCmd);

			// "HW.Reg" family
			// Format L-vars: "HW.RegLF[RWB].", "HW.RegLV[RWB]."
			// Format X-vars: "HW.RegXI[RWB].", "HW.RegXF[RWB].", "HW.RegXS[RWB].", "HW.RegXV[RWB]."
			// [RWB]:	'R'ead, 'W'rite or 'B'oth
			// ValType:	'F' = FLOAT64, 'I' = INT32, 'S' = STRING256, 'V' = VOID
			if (strncmp(sCmd, "HW.Reg", 6) == 0)
			{
				RegisterVar(&sCmd[10], sCmd[6], sCmd[7], sCmd[8]);
				break;
			}

			// X-Vars or L-Vars that can not be set through Event (FLOAT64 or STRING256 parameters)
			// Format: NNNN=[parameter]
			// NNNN is same as EventID (index of Vars increased by EVENT_CUSTOM_START)
			if (sCmd[4] == '=')
			{
				int i = atoi(sCmd);
				VarData v = Vars[i - EVENT_CUSTOM_START];

				if (v.VarType == 'L')
				{
					FLOAT64 f64 = atof(&sCmd[5]);
					set_named_variable_value(v.lvID, f64);
					fprintf(stderr, "%s: RECV_CLIENT_DATA: set_named_variable_value(%u, %f)", WASM_Name, v.lvID, f64);
				}
				else if (v.VarType = 'X')
				{
					// Build string and execute
					char sExe[256 + 256];
					sprintf(sExe, "%s %s", &sCmd[5], v.Name);
					execute_calculator_code(sExe, nullptr, nullptr, nullptr);
					fprintf(stderr, "%s: RECV_CLIENT_DATA: \"%s\"", WASM_Name, sExe);
				}
				break;
			}

			// "HW.Exe."
			if (strncmp(sCmd, "HW.Exe.", 7) == 0)
			{
				ExecuteCalculatorCode(&sCmd[7]);
				break;
			}
		}

		break; // end case SIMCONNECT_RECV_ID_CLIENT_DATA
	}

	case SIMCONNECT_RECV_ID_EVENT_FRAME:
	{
		ReadVars();
		break; // end case SIMCONNECT_RECV_ID_EVENT_FRAME
	}

	default:
		break; // end case default
	}
}

void RegisterClientDataArea()
{
	HRESULT hr;

	// Create Client Data Area for commands
	// Max size of string in SimConnect is 256 characters (including the '/0')
	// If longer commands are required, then they would need to be split in several strings
	hr = SimConnect_MapClientDataNameToID(g_hSimConnect, CLIENT_DATA_NAME_COMMAND, CLIENT_DATA_ID_COMMAND);
	if (hr != S_OK) {
		fprintf(stderr, "%s: Error creating Client Data Area %s. %u", WASM_Name, CLIENT_DATA_NAME_COMMAND, hr);
		return;
	}
	SimConnect_CreateClientData(g_hSimConnect, CLIENT_DATA_ID_COMMAND, MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

	// Create Client Data Area to acknowledge a registration back to the client
	hr = SimConnect_MapClientDataNameToID(g_hSimConnect, CLIENT_DATA_NAME_ACKNOWLEDGE, CLIENT_DATA_ID_ACKNOWLEDGE);
	if (hr != S_OK) {
		fprintf(stderr, "%s: Error creating Client Data Area %s. %u", WASM_Name, CLIENT_DATA_NAME_ACKNOWLEDGE, hr);
		return;
	}
	SimConnect_CreateClientData(g_hSimConnect, CLIENT_DATA_ID_ACKNOWLEDGE, sizeof(VarData), SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

	// Create Client Data Area for result of execute_calculator_code
	hr = SimConnect_MapClientDataNameToID(g_hSimConnect, CLIENT_DATA_NAME_RESULT, CLIENT_DATA_ID_RESULT);
	if (hr != S_OK) {
		fprintf(stderr, "%s: Error creating Client Data Area %s. %u", WASM_Name, CLIENT_DATA_NAME_RESULT, hr);
		return;
	}
	SimConnect_CreateClientData(g_hSimConnect, CLIENT_DATA_ID_RESULT, sizeof(Result), SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

	// This Data Definition will be used with the COMMAND Client Area Data
	hr = SimConnect_AddToClientDataDefinition(
		g_hSimConnect,
		DATA_DEFINITION_ID_STRING_COMMAND,
		0,				// Offset
		MESSAGE_SIZE	// Size
	);

	// This Data Definition will be used with the ACKNOWLEDGE Client Area Data
	hr = SimConnect_AddToClientDataDefinition(
		g_hSimConnect,
		DATA_DEFINITION_ID_ACKNOWLEDGE,
		0,				// Offset
		sizeof(VarData)	// Size
	);

	// This Data Definition will be used with the RESULT Client Area Data
	hr = SimConnect_AddToClientDataDefinition(
		g_hSimConnect,
		DATA_DEFINITION_ID_RESULT,
		0,				// Offset
		sizeof(Result)	// Size
	);

	// We immediately start to listen to commands coming from the client
	SimConnect_RequestClientData(
		g_hSimConnect,
		CLIENT_DATA_ID_COMMAND,						// ClientDataID
		CMD,										// RequestID
		DATA_DEFINITION_ID_STRING_COMMAND,			// DefineID
		SIMCONNECT_CLIENT_DATA_PERIOD_ON_SET,		// Trigger when data is set
		SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT	// Always receive data
	);

	// Give highest priority to HABI_WASM_GROUP to deal with Events
	hr = SimConnect_SetNotificationGroupPriority(g_hSimConnect, HABI_WASM_GROUP::GROUP, SIMCONNECT_GROUP_PRIORITY_HIGHEST);
	if (hr != S_OK)
	{
		fprintf(stderr, "%s: SimConnect_SetNotificationGroupPriority failed.\n", WASM_Name);
		return;
	}
}

extern "C" MSFS_CALLBACK void module_init(void)
{
	HRESULT hr;

	g_hSimConnect = 0;

	fprintf(stderr, "%s: Initializing WASM version [%s]", WASM_Name, WASM_Version);

	hr = SimConnect_Open(&g_hSimConnect, WASM_Name, (HWND)NULL, 0, 0, 0);
	if (hr != S_OK)
	{
		fprintf(stderr, "%s: SimConnect_Open failed.\n", WASM_Name);
		return;
	}

	hr = SimConnect_SubscribeToSystemEvent(g_hSimConnect, EVENT_FRAME, "Frame");
	if (hr != S_OK)
	{
		fprintf(stderr, "%s: SimConnect_SubsribeToSystemEvent \"Frame\" failed.\n", WASM_Name);
		return;
	}

	hr = SimConnect_CallDispatch(g_hSimConnect, MyDispatchProc, NULL);
	if (hr != S_OK)
	{
		fprintf(stderr, "%s: SimConnect_CallDispatch failed.\n", WASM_Name);
		return;
	}

	RegisterClientDataArea();

	fprintf(stderr, "%s: Initialization completed", WASM_Name);
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
	fprintf(stderr, "%s: De-initializing WASM version [%s]", WASM_Name, WASM_Version);

	if (!g_hSimConnect)
	{
		fprintf(stderr, "%s: SimConnect handle was not valid.\n", WASM_Name);
		return;
	}

	HRESULT hr = SimConnect_Close(g_hSimConnect);
	if (hr != S_OK)
	{
		fprintf(stderr, "%s: SimConnect_Close failed.\n", WASM_Name);
		return;
	}

	fprintf(stderr, "%s: De-initialization completed", WASM_Name);
}
