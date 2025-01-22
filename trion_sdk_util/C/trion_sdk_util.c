/**
 * Common Functions used in SDK Examples
 */

#include <stdio.h>
#include <string.h>
#ifdef USE_TRIONET_API
#  include "dewepxinet_load.h"
#else
#  include "dewepxi_load.h"
#endif
#include "dewepxi_apicore.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"
#ifdef WIN32
#include "dewepxi_apiutil.h"
#else
#include <stdio.h>
#endif

// out-comment the undef, to get warnings reported to console
#define REVEAL_WARNINGS
//#undef REVEAL_WARNINGS



/**
* Load TRION dynamic library at the begin of the examples
* @return 0 in case of success.
*/
int LoadTrionApi()
{
    if (!DeWePxiLoad())
    {
        printf("pxi_api.dll could not be found. Exiting...\n");
        return 1;
    }
    return 0;
}


/**
* Function to shut-down application in case of an error or at end of example
* ErrorTxt may be Null (no additional output will happen)
* Unloads the TRION dynamic library.
* @param optional error text. Can be NULL
* @return 0 in case of success.
*/
int UnloadTrionApi(const char* sErrorTxt )
{
    if ( NULL != sErrorTxt)
    {
        fprintf(stderr, "%s", sErrorTxt);
        fflush(stderr);
    }

    DeWePxiUnload();
    return 0;
}


/**
 * Translate an error-code to human readable form
 * @param nErrorCode
 * @return TRUE, if error-code is an error
 * @return FALSE, if passed error-code is OK, or only warning
 */
BOOL CheckError(int nErrorCode)
{
    if (nErrorCode > 0)
    {
        fprintf(stderr, "\nError: %s\n", DeWeErrorConstantToString(nErrorCode));
        fflush(stderr);
        return TRUE;
    }
    else
    {
#ifdef REVEAL_WARNINGS
        if ( nErrorCode != 0 )
        {
            fprintf(stderr, "\nError: %s\n", DeWeErrorConstantToString(nErrorCode));
            fflush(stderr);
        }
#endif
        return FALSE;
    }
}


/**
 * Check if the BoardID  is set by the command line. If not, use default ID "0"
 * @param argc program argument count
 * @param argv array of program arguments
 * @param nNoOfBoards is the number of detected TRION boards
 * @param nBoardId is the parsed board id from command line
 * @return TRUE is the board id is valid
 */
BOOL ARG_GetBoardId(int argc, char **argv, int nNoOfBoards, int *nBoardId)
{
    if (argc > 1)
    {
        sscanf(argv[1], "%d", nBoardId);

        if (*nBoardId >= nNoOfBoards)
        {
            return FALSE;
        }
    }
    else
    {
        // No CMdline Argument -> Default Board ID
        *nBoardId = 0;
    }
    return TRUE;
}


/**
* Check if the BoardID  is set by the command line. If not, use default ID "0" and "1"
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param nBoardId1 is the parsed first board id from command line
* @param nBoardId2 is the parsed second board id from command line
* @return TRUE is the board id is valid
*/
BOOL ARG_GetBoardIdEX(int argc, char **argv, int nNoOfBoards, int *nBoardId1, int *nBoardId2)
{
    if (argc > 2)
    {
        sscanf(argv[1], "%d", nBoardId1);
        sscanf(argv[2], "%d", nBoardId2);

        if ( (*nBoardId1 >= nNoOfBoards) || (*nBoardId2 >= nNoOfBoards) )
        {
            return FALSE;
        }
    }
    else
    {
        // No CMdline Argument2 -> Default Board IDs "0" and "1"
        *nBoardId1 = 0;
        *nBoardId2 = 1;
    }
    return TRUE;
}

/**
* Check if the ChannelID is set by the command line. If not, use default ChannelId "0"
* Possible ChannelID range [0..7]
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param ChannelNo is the parsed channel id from command line
* @return TRUE is the channel id is valid
*/
BOOL ARG_GetChannelNo(int argc, char **argv, int nNoOfBoards, int *ChannelNo)
{
    if (argc > 2)
    {
        sscanf(argv[2], "%d", ChannelNo);
        if ((*ChannelNo > 7) || (*ChannelNo < 0))
        {
           return FALSE;
        }
    }
    else
    {
        *ChannelNo = 0;
    }
    return TRUE;
}

/**
* Check if two ChannelIDs are set by the command line. If not, use default ChannelId "0" and "1"
* Possible ChannelID range [0..7]
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param ChannelNo1 is the first parsed channel id from command line
* @param ChannelNo2 is the second parsed channel id from command line
* @return TRUE is the channel id is valid
*/
BOOL ARG_GetChannelNoEX(int argc, char **argv, int nNoOfBoards, int *ChannelNo1, int *ChannelNo2)
{
    if (argc > 3)
    {
        sscanf(argv[2], "%d", ChannelNo1);
        if ((*ChannelNo1 > 7) || (*ChannelNo1 < 0))
        {
            return FALSE;
        }
        sscanf(argv[3], "%d", ChannelNo2);
        if ((*ChannelNo2 > 7) || (*ChannelNo2 < 0))
        {
            return FALSE;
        }
    }
    else
    {
        // one channel set over cmd line
        if (argc > 2)
        {
            sscanf(argv[2], "%d", ChannelNo1);
            if ((*ChannelNo1 > 7) || (*ChannelNo1 < 0))
            {
                return FALSE;
            }
            *ChannelNo2 = 0;

        }
        // 2 channels but not set over cmd line
        else
        {
            *ChannelNo1 = 0;
            *ChannelNo2 = 1;
        }
    }
    return TRUE;
}


static BOOL arg_GetOptionValue(int argc, char** argv, int i, char** v)
{
    if (i < argc)
    {
        *v = argv[i];
        return TRUE;
    }

    return FALSE;
}


/**
 * Check for the given option in the form "--option value".
 * Use this function like this ARG_GetOption(argc, argv, "--option", buffer, sizeof(buffer)).
 * @param argc program argument count
 * @param argv array of program arguments
 * @param param is the name of the option. Prefixes have to be part of the string.
 * @param value is the value of the option ( a string buffer)
 * @param value_size is the size of the string buffer.
 * @return TRUE is the the option was found and a value returned.
 */
BOOL ARG_GetOption(int argc, char** argv, const char* param, char* value, size_t value_size)
{
    BOOL ret = FALSE;
    int i;
    char* v = NULL;

    for (i = 1; i < argc; ++i)
    {
        if (0 == strncmp(argv[i], param, strlen(param)))
        {
            if (arg_GetOptionValue(argc, argv, i+1, &v))
            {
                strncpy(value, v, value_size);
                return TRUE;
            }
        }
    }

    return ret;
}


/**
 * Check for the given boolean option in the form "--option".
 * Use this function like this ARG_GetOption(argc, argv, "--option", buffer, sizeof(buffer)).
 * @param argc program argument count
 * @param argv array of program arguments
 * @param param is the name of the option. Prefixes have to be part of the string.
 * @return TRUE is the the option was found and a value returned.
 */
BOOL ARG_GetBooleanOption(int argc, char** argv, const char* param)
{
    int i;
    char* v = NULL;

    for (i = 1; i < argc; ++i)
    {
        if (0 == strncmp(argv[i], param, strlen(param)))
        {
            return TRUE;
        }
    }

    return FALSE;
}

/**
 * If the board given by BoardID is a board that can be used for this example.
 * The comparison is only performed on the length of the entries in the list (so it can be fuzzy).
 * Example: "TRION-2402-dSTG" will accept all installed connector-panel types of TRION-dSTG boards like
 * TRION-2402-dSTG-8A.SB, TRION2406-dSTG-6LE, etc...
 * @param nBoardID the id of the board to check the name
 * @param sBoardNameNeeded list of allowed board names for the test
 * @return TRUE if the board matches the list.
 */
BOOL TestBoardType( int nBoardID, const char **sBoardNameNeeded )
{
    char sBoardName[256] = {0};
    char sBoardID[256]   = { 0 };
    int i=0;
    int nErrorCode = ERR_NONE;

    // build string target id
    snprintf(sBoardID, sizeof(sBoardID), "BoardID%d", nBoardID);

    // Request the TRION board name
    nErrorCode = DeWeGetParamStruct_str(sBoardID, "BoardName", sBoardName, sizeof(sBoardName));
    if ( 0 != nErrorCode )
    {
        CheckError(nErrorCode);
        return FALSE;
    }

    for ( i = 0; NULL != sBoardNameNeeded[i]; ++i )
    {
        if( strncmp(sBoardName, sBoardNameNeeded[i], min(sizeof(sBoardName), strlen(sBoardNameNeeded[i]))) == 0 )
        {
            fprintf( stderr, "Found a %s board with BoardID %d\n", sBoardName, nBoardID);
            fflush(stderr);
            return TRUE;
        }
    }

    fprintf( stderr, "BoardID %d (Boardname: %s) is not any of the required boards.\nList of possible boards for this example:\n", nBoardID, sBoardName);
    fflush(stderr);
    for ( i = 0; NULL != sBoardNameNeeded[i]; ++i )
    {
        printf("  %s\n", sBoardNameNeeded[i]);
    }
    return FALSE;
}


/**
 * Set scale parameter from TRION API data
 * Loose of precision because char -> double conversation
 * @param scaleinfo pointer to the scalinfo object (== this)
 * @param target
 * @return TRION API error code
 */
int SetScaling(ScaleInfo* scaleinfo, const char* target)
{
    int nErrorCode = 0;
    char fromBd[256] = { 0 };

    // Get Scale Value
    nErrorCode = DeWeGetParamStruct_str(target, "scalevalue", fromBd, sizeof(fromBd));
    CheckError(nErrorCode);
    scaleinfo->fScaling = DblStr2Dbl(fromBd);

    // Get Offset Value
    nErrorCode = DeWeGetParamStruct_str(target, "scaleoffset", fromBd, sizeof(fromBd));
    CheckError(nErrorCode);
    scaleinfo->fd = DblStr2Dbl(fromBd);

    return nErrorCode;
}


/**
* Scale received measurement data according to adjusted range.
* In principle the same algorithm as used in TRION API, but no char to double conversation
* and thus more precision.
* @param scaleinfo pointer to the scalinfo object (== this)
* @param minrange
* @param maxrange
* @param BitWidth
* @return TRION API error code
*/
int CalcScaling(ScaleInfo* scaleinfo, double minrange, double maxrange, int BitWidth)
{
    double fSpan=0.0f;
    double fk=0.0f;
    double fd=0.0f;
    uint32   dwMinVal=0;
    uint32   dwMaxVal=0;
    double fScaling=0.0f;

    fSpan = maxrange - minrange;
    fk = (double)(( fSpan ) / 2 );
    fd = fk - maxrange;

    switch (BitWidth)
    {
        case 24:
            dwMinVal = (uint32)(1 << (24 - 1) );
            break;
        case 16:
            dwMinVal = (uint32)(1 << (16 - 1) );
            break;
    }
    dwMaxVal = dwMinVal - 1;
    dwMinVal *= -1;

    fScaling= fk / dwMaxVal;

    scaleinfo->fScaling = fScaling;
    scaleinfo->fd = fd;
    scaleinfo->fSpan = fSpan;
    scaleinfo->fk = fk;

    return ERR_NONE;
}



/**
 * Format the Raw Data read from API, according to the adjusted BitWidth.
 * @param RawValue sample value direct from dma buffer
 * @param BitWidth bit width if the sample value
 * @return the converted value to signed int.
 */
signed int formatRawData(signed int RawValue, int BitWidth, int offset)
{
    switch (BitWidth)
    {
    case 16:
        return RawValue;
        break;

    case 24:
        if (offset != 0)
        {
            if (RawValue < 0)
            {
                RawValue *= -1;
                RawValue = RawValue >> offset;
                RawValue *= -1;
            }
            else
            {
                RawValue = RawValue >> offset;
            }
        }
        else
        {
            if (RawValue & 0x800000)
            {
                RawValue = RawValue | 0xff000000;
            }
            else
            {
                RawValue = RawValue & 0x00ffffff;
            }
        }

        return RawValue;
        break;

    case 32:
        return RawValue;
        break;
    }

    return RawValue;

}

/**
 * Conversion from const char* to double value
 * @param Val is the string representation of the value
 * @return the value as double
 */
double DblStr2Dbl(const char* Val)
{
    double nData = 0.0f;

    sscanf(Val, "%lf", &nData);
    return nData;
}


/**
* @brief
*
* @param  szString      String holding Input-Data
* @param  szSub1        Pointer to 1st SubString
* @param  szSub2        Pointer to 2nd SubString. NULL if not in format 'min..max' -> symmetrical range
* @return const char*   malloced(!) copy of input-string, chopped with terminating \0, so that szSub1 and szSub2 point
*                       to the substrings (part before and after .. of 'min..max')
*/
static const char* split_range(const char* szString, char** szSub1, char** szSub2)
{
    char* szRet = strdup(szString);
    if (NULL == szRet) {
        return NULL;
    }

    *szSub1 = (char*)szRet;
    *szSub2 = strstr(szRet, "..");
    if (NULL != *szSub2) {
        (*szSub2)[0] = '\0';
        if ((*szSub2)[2] != '\0') {
            //Move Pointer, if within bounds
            (*szSub2) += 2;
        }
        else {
            *szSub2 = NULL;
        }
    }
    return szRet;
}


/**
 * Convert Boards/channels range (const char*) to double value
 * @param sTarget
 * @return range as double
 */
int GetAdjustedRange(const char* sTarget, RangeSpan* rangespan)
{
    int nErrorCode = 0;
    char adj_range[48] = {0};
    char val_str[48]   = {0};
    char*   szPart1 = NULL; //Left side part of the String (before '..')
    char*   szPart2 = NULL; //Right side Part of the String ( after '..' );
    char*   pEnd;
    double dval;

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Range", adj_range, sizeof(adj_range));
    CheckError(nErrorCode);

    //Split up string in substrings. Warning-> szLocal is a malloced Buffer an needs free
    char* szLocal = (char*)split_range(adj_range, &szPart1, &szPart2);
    if (NULL == szLocal) {
        return -1;
    }
    /*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    From here on szLocal has to be freed on each return
    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
    //szPart1 mandatory
    if (NULL == szPart1)
    {
        free(szLocal);
        return -1;
    }
    dval = strtod(szPart1, &pEnd);
    if (NULL != szPart2)
    {
        //asymmetrical range
        rangespan->rmin = dval;
        rangespan->rmax = dval = strtod(szPart2, &pEnd);
    }
    else {
        //symmetrical range
        rangespan->rmin = (-1.0) * dval;
        rangespan->rmax = dval;
    }

    free(szLocal);

    return 0;
}


/**
 * Get reference maximum value for selected Board.
 * In real-world this would be done by processing the properties-xml.
 * for the SDK examples, a simple, hard-coded lookup table is used.
 * @param nBoardID
 * @param sBoardID
 * @param sBoardNameNeeded
 * @param maxArefVal
 * @return the maximum value.
 */
double GetMaxARef( int nBoardID, const char *sBoardID, const char **sBoardNameNeeded, const double *maxArefVal )
{
    char sBoardName[256]={0};
    int i=0;
    int nErrorCode = ERR_NONE;


    // Request the TRION board name
    nErrorCode = DeWeGetParamStruct_str(sBoardID, "BoardName", sBoardName, sizeof(sBoardName));
    CheckError(nErrorCode);

    for ( i = 0; NULL != sBoardNameNeeded[i]; ++i )
    {
        if( strncmp(sBoardName, sBoardNameNeeded[i], min(sizeof(sBoardName), strlen(sBoardNameNeeded[i]))) == 0 )
        {
            return maxArefVal[i];
        }
    }

    return 0.0f;
}


/**
 * Dump and pretty print the xml tree.
 */
void DumpXmlTree(const char* tree)
{
    const char* pc = NULL;
    int  ixml_indent = 0;
    int bNewLine = 0;

    pc = tree;
    while ('\0' != *pc)
    {
        if ('>' == *pc){
            bNewLine = 1;
            printf(">\n%*s", (ixml_indent * 2), "");
        }
        else if ('<' == *pc) {
            if ('/' == *(pc + 1)){
                ++pc;
                --ixml_indent;
                if (!bNewLine){
                    printf("\n%*s</", (ixml_indent * 2), "");
                }
                else {
                    printf("</");
                }
                bNewLine = 1;
            }
            else {
                if (!bNewLine){
                    printf("%*s<", (ixml_indent * 2), "");
                }
                else {
                    printf("<");
                }
                ++ixml_indent;
            }
        }
        else if ('\n' == *pc) {
            //silently swallow
        }
        else if ('/' == *pc) {
            if ('>' == *(pc + 1)) {
                ++pc;
                --ixml_indent;
                bNewLine = 1;
                printf("/>\n%*s", (ixml_indent * 2), "");
            }
            else {
                printf("%c", *pc);
                bNewLine = 0;
            }
        }
        else {
            printf("%c", *pc);
            bNewLine = 0;
        }
        ++pc;
    }
}


void ListNetworkInterfaces()
{
    int nErrorCode = 0;

    // List available network interfaces
    char netIfXML[10000];
    nErrorCode = DeWeGetParamStruct_str("trionetapi", "Network/Enumerate", netIfXML, sizeof(netIfXML));
    CheckError(nErrorCode);

    DumpXmlTree(netIfXML);
}


typedef struct PathCodeMapping_tag
{
    TRION_PATH_ENUM key;
    const char*     value;
} PathCodeMapping;

static PathCodeMapping s_path_key_map[] =
{
    { TRION_CONFIG_PATH ,       "config"},
    { TRION_LOG_PATH,           "log" },
    { TRION_SYSTEM_XML_PATH,    "systemxml" },
    { TRION_TEST_RESULTS_PATH,  "testresults" },
    { TRION_BACKUP_PATH,        "backup" }
};


/**
* Get the configuration/log/.. path used by TRION API.
* This is the path where the essential API configuration files
* are located (eg dwpxi_api_config.xml).
* @param path is a pointer to the string buffer for the path
* @param max_path is the size of the string buffer.
* @return TRION API error code
*/
int GetApiPath(TRION_PATH_ENUM path_code, char* path, size_t max_path)
{
    unsigned int path_size;
    int nErrorCode;
    int i;
    PathCodeMapping* entry = NULL;
    char target[256] = { 0 };

    // look for a valid "command" name
    for (i = 0; i < sizeof(s_path_key_map) / sizeof(PathCodeMapping); ++i)
    {
        if (s_path_key_map[i].key == path_code)
        {
            entry = &s_path_key_map[i];
            break;
        }
    }

    if (entry != NULL)
    {
        // Get the API config path for info
        snprintf(target, sizeof(target), "System/%s", entry->value);

        nErrorCode = DeWeGetParamStruct_strLEN(target, "Path", &path_size);
        CheckError(nErrorCode);
        if (!nErrorCode)
        {
            if (path_size < max_path)
            {
                nErrorCode = DeWeGetParamStruct_str(target, "Path", path, path_size + 1);
                CheckError(nErrorCode);
                return ERR_NONE;
            }
            else
            {
                return ERROR_BUFFER_TOO_SMALL;
            }
        }

    }

    return ERR_PARAM_INVALID;

}
int TRION_GetBoardName(int nBoardID, char* sBoardName, int len)
{
    static char sBoardID[256] = {0};
    int nErrorCode = 0;
    snprintf(sBoardID, sizeof(sBoardID), "BoardID%d", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sBoardID, "BoardName", sBoardName, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

int TRION_GetNrOfChannelsAI(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sNumChannels[8] = {0};
    int nErrorCode=0;
    int nNumChannels = 0;
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Channels", sNumChannels, sizeof(sNumChannels));
    CheckError(nErrorCode);

    sscanf(sNumChannels, "%d", &nNumChannels);
    return nNumChannels;
}

int TRION_GetNrOfChannelsCNT(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sNumChannels[8] = {0};
    int nErrorCode=0;
    int nNumChannels = 0;
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/CNT", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Channels", sNumChannels, sizeof(sNumChannels));
    CheckError(nErrorCode);

    sscanf(sNumChannels, "%d", &nNumChannels);
    return nNumChannels;
}

int TRION_GetNrOfChannelsDI(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sNumChannels[8] = {0};
    int nErrorCode=0;
    int nNumChannels = 0;
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/Discret", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Channels", sNumChannels, sizeof(sNumChannels));
    CheckError(nErrorCode);

    sscanf(sNumChannels, "%d", &nNumChannels);
    return nNumChannels;
}

int TRION_GetNrOfChannelsBoardCNT(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sNumChannels[8] = {0};
    int nErrorCode=0;
    int nNumChannels = 0;
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardCNT", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Channels", sNumChannels, sizeof(sNumChannels));
    CheckError(nErrorCode);

    sscanf(sNumChannels, "%d", &nNumChannels);
    return nNumChannels;
}

int TRION_GetNrOfChannelsUART(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sNumChannels[8] = {0};
    int nErrorCode=0;
    int nNumChannels = 0;
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/UART", nBoardID);

    nErrorCode = DeWeGetParamStruct_str(sTarget, "Channels", sNumChannels, sizeof(sNumChannels));
    CheckError(nErrorCode);

    sscanf(sNumChannels, "%d", &nNumChannels);
    return nNumChannels;
}

int TRION_AcqProp_GetMinSampleRate(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sSampleRate[8] = {0};
    int nErrorCode=0;
    int nSampleRate = 0;

    // Access the entry in thhe BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties/AcquisitionProperties/AcqProp", nBoardID);

    nErrorCode = DeWeGetParamXML_str(sTarget, "SampleRate/@ProgMin", sSampleRate, sizeof(sSampleRate));
    CheckError(nErrorCode);

    sscanf(sSampleRate, "%d", &nSampleRate);
    return nSampleRate;
}

int TRION_AcqProp_GetMaxSampleRate(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sSampleRate[8] = {0};
    int nErrorCode=0;
    int nSampleRate = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties/AcquisitionProperties/AcqProp", nBoardID);

    nErrorCode = DeWeGetParamXML_str(sTarget, "SampleRate/@ProgMax", sSampleRate, sizeof(sSampleRate));
    CheckError(nErrorCode);

    sscanf(sSampleRate, "%d", &nSampleRate);
    return nSampleRate;
}

int TRION_AcqProp_GetNumResolutionAI(int nBoardID)
{
    static char sTarget[256] = {0};
    static char sCount[8] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported Resolutions
    nErrorCode = DeWeGetParamXML_str(sTarget, "count(AcquisitionProperties/AcqProp/ResolutionAI/*[starts-with(local-name(), 'ID')])", sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_AcqProp_GetResolutionAI(int nBoardID, int index)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sResolution[8] = {0};
    int nErrorCode=0;
    int nResolution = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties/AcquisitionProperties/AcqProp", nBoardID);

    snprintf(sCommand, sizeof(sCommand), "ResolutionAI/ID%d", index);

    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sResolution, sizeof(sResolution));
    CheckError(nErrorCode);

    sscanf(sResolution, "%d", &nResolution);
    return nResolution;
}

int TRION_ChanProp_GetNumModesAI(int nBoardID, int chan_index)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sCount[8] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported measurement modes of channel AI*
    snprintf(sCommand, sizeof(sCommand), "count(ChannelProperties/AI%d/Mode)", chan_index);
    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_ChanProp_GetModeNameAI(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    int nErrorCode=0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // xpath index start with 1
    snprintf(sCommand, sizeof(sCommand), "ChannelProperties/AI%d/Mode[%d]/@Mode", chan_index, mode_index + 1);

    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sBuffer, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

int TRION_ChanProp_GetNumModesCNT(int nBoardID, int chan_index)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sCount[8] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported measurement modes of channel CNT*
    snprintf(sCommand, sizeof(sCommand), "count(ChannelProperties/CNT%d/Mode)", chan_index);
    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_ChanProp_GetModeNameCNT(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    int nErrorCode=0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // xpath index start with 1
    snprintf(sCommand, sizeof(sCommand), "ChannelProperties/CNT%d/Mode[%d]/@Mode", chan_index, mode_index + 1);

    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sBuffer, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

int TRION_ChanProp_GetNumModesDI(int nBoardID, int chan_index)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sCount[8] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported measurement modes of channel DI*
    snprintf(sCommand, sizeof(sCommand), "count(ChannelProperties/Discret%d/Mode)", chan_index);
    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_ChanProp_GetModeNameDI(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    int nErrorCode=0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // xpath index start with 1
    snprintf(sCommand, sizeof(sCommand), "ChannelProperties/Discret%d/Mode[%d]/@Mode", chan_index, mode_index + 1);

    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sBuffer, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

int TRION_ChanProp_GetNumModesBoardCNT(int nBoardID, int chan_index)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sCount[8] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported measurement modes of channel BoardCNT*
    snprintf(sCommand, sizeof(sCommand), "count(ChannelProperties/BoardCNT%d/Mode)", chan_index);
    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_ChanProp_GetModeNameBoardCNT(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    int nErrorCode=0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // xpath index start with 1
    snprintf(sCommand, sizeof(sCommand), "ChannelProperties/BoardCNT%d/Mode[%d]/@Mode", chan_index, mode_index + 1);

    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sBuffer, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

int TRION_ChanProp_GetNum(int nBoardID, int chan_index, const char* ch_name, const char* mode, const char* prop)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    static char sCount[10000] = {0};
    int nErrorCode=0;
    int nCount = 0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // Use xpath count function to count the number of supported measurement modes of channel AI*
    snprintf(sCommand, sizeof(sCommand), "count(ChannelProperties/%s%d/Mode[@Mode='%s']/%s/*[starts-with(local-name(), 'ID')])", ch_name, chan_index, mode, prop);
    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sCount, sizeof(sCount));
    CheckError(nErrorCode);

    sscanf(sCount, "%d", &nCount);
    return nCount;
}

int TRION_ChanProp_GetEntry(int nBoardID, int chan_index, const char* ch_name, const char* mode, const char* prop, int index, char* sBuffer, int len)
{
    static char sTarget[256] = {0};
    static char sCommand[256] = {0};
    int nErrorCode=0;

    // Access the entry in the BoardX_Properties.xml file using a XPATH expression
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardProperties", nBoardID);

    // access ID elements
    snprintf(sCommand, sizeof(sCommand), "ChannelProperties/%s%d/Mode[@Mode='%s']/%s/ID%d", ch_name, chan_index, mode, prop, index);


    nErrorCode = DeWeGetParamXML_str(sTarget, sCommand, sBuffer, len);
    CheckError(nErrorCode);
    return nErrorCode;
}

#ifdef WIN32

#define US_PER_SECOND 1000000

typedef struct 
{
    LARGE_INTEGER m_time_start;
    LARGE_INTEGER m_time_freq;
    uint64 m_last_time;
} TRION_StopWatchHandleImp;

void TRION_StopWatch_Create(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp*)malloc(sizeof(TRION_StopWatchHandleImp));
    QueryPerformanceFrequency(&handle_imp->m_time_freq);
    *sw = handle_imp;
}

void TRION_StopWatch_Destroy(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp * )*sw;
    free(handle_imp);
}

void TRION_StopWatch_Start(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp*)(sw);
    QueryPerformanceCounter(&handle_imp->m_time_start);
}

void TRION_StopWatch_Stop(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp*)(sw);
    LARGE_INTEGER time_end;
    QueryPerformanceCounter(&time_end);
    if (handle_imp->m_time_freq.QuadPart != 0)
    {
        uint64 time_diff = time_end.QuadPart - handle_imp->m_time_start.QuadPart;
        if (handle_imp->m_time_freq.QuadPart > US_PER_SECOND)
        {
            time_diff = (time_diff * US_PER_SECOND) / handle_imp->m_time_freq.QuadPart;
        }
        handle_imp->m_last_time = time_diff;
    }
}

uint64 TRION_StopWatch_GetUS(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp*)(sw);
    return handle_imp->m_last_time;
}

uint64 TRION_StopWatch_GetMS(TRION_StopWatchHandle* sw)
{
    TRION_StopWatchHandleImp* handle_imp = (TRION_StopWatchHandleImp*)(sw);
    return handle_imp->m_last_time / 1000;
}

#else
void TRION_StopWatch_Create(TRION_StopWatchHandle* sw)
{
}

void TRION_StopWatch_Destroy(TRION_StopWatchHandle* sw)
{
}
void TRION_StopWatch_Start(TRION_StopWatchHandle* sw)
{
}

void TRION_StopWatch_Stop(TRION_StopWatchHandle* sw)
{
}

uint64 TRION_StopWatch_GetUS(TRION_StopWatchHandle* sw)
{
    return 0;
}

uint64 TRION_StopWatch_GetMS(TRION_StopWatchHandle* sw)
{
    return 0;
}
#endif


// Simple internalized DB

double MSI_GetMinRange(const char* msi_type)
{
    if (0 == strcmp(msi_type, "MSI-BR-ACC"))
    {
        return -10000;
    }
    if (0 == strcmp(msi_type, "MSI-BR-V-200"))
    {
        return -200;
    }
    if (0 == strcmp(msi_type, "MSI2-CH-5"))
    {
        return -5000;
    }
    if (0 == strcmp(msi_type, "MSI2-CH-100"))
    {
        return -100000;
    }
    if (0 == strcmp(msi_type, "MSI2-STG-5V"))
    {
        return -20;
    }
    if (0 == strcmp(msi_type, "MSI2-STG-10V"))
    {
        return -10;
    }
    if (0 == strcmp(msi_type, "MSI2-V-600"))
    {
        return -1000;
    }

    // Unknown MSI:
    return 0;
}

double MSI_GetMaxRange(const char* msi_type) 
{
    if (0 == strcmp(msi_type, "MSI-BR-ACC"))
    {
        return 10000;
    }
    if (0 == strcmp(msi_type, "MSI-BR-V-200"))
    {
        return 200;
    }
    if (0 == strcmp(msi_type, "MSI2-CH-5"))
    {
        return 5000;
    }
    if (0 == strcmp(msi_type, "MSI2-CH-100"))
    {
        return 100000;
    }
    if (0 == strcmp(msi_type, "MSI2-STG-5V"))
    {
        return 20;
    }
    if (0 == strcmp(msi_type, "MSI2-STG-10V"))
    {
        return 10;
    }
    if (0 == strcmp(msi_type, "MSI2-V-600"))
    {
        return 1000;
    }
    // Unknown MSI:
    return 0;
}

const char* MSI_GetMinRangeUnit(const char* msi_type)
{
    return MSI_GetMaxRangeUnit(msi_type);
}

const char* MSI_GetMaxRangeUnit(const char* msi_type)
{
    if (0 == strcmp(msi_type, "MSI-BR-ACC"))
    {
        return "mV";
    }
    if (0 == strcmp(msi_type, "MSI-BR-V-200"))
    {
        return "V";
    }
    if (0 == strcmp(msi_type, "MSI2-CH-5"))
    {
        return "pC";
    }
    if (0 == strcmp(msi_type, "MSI2-CH-100"))
    {
        return "pC";
    }
    if (0 == strcmp(msi_type, "MSI2-STG-5V"))
    {
        return "mV/V";
    }
    if (0 == strcmp(msi_type, "MSI2-STG-10V"))
    {
        return "mV/V";
    }
    if (0 == strcmp(msi_type, "MSI2-V-600"))
    {
        return "V";
    }
    return "";
}
