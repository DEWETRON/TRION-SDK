/**
 * 
 * Common Functions used in SDK Examples
 *  
 */

#ifndef TRION_SDK_UTIL_FILE
#define TRION_SDK_UTIL_FILE


#ifndef min
#define min(a,b) (((a) < (b)) ? (a) : (b))
#define max(a,b) (((a) > (b)) ? (a) : (b))
#endif



/**
 * Load TRION dynamic library ar the begin of the examples
 * @return 0 in case of success.
 */
int LoadTrionApi();

/**
 * Function to shut-down application in case of an error or at end of example
 * ErrorTxt may be Null (no additional output will happen)
 * Unloads the TRION dynamic library.
 * @param optional error text. Can be NULL
 * @return 0 in case of success.
 */
int UnloadTrionApi(const char* error);

/**
 * Translate an error-code to human readable form
 * @param nErrorCode
 * @return TRUE, if error-code is an error
 * @return FALSE, if passed error-code is OK, or only warning
 */
BOOL CheckError(int nErrorCode);

/**
* Check if the BoardID  is set by the command line. If not, use default ID "0"
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param nBoardId is the parsed board id from command line
* @return TRUE is the board id is valid
*/
BOOL ARG_GetBoardId(int argc, char **argv, int nNoOfBoards, int *nBoardId);

/**
* Check if the BoardID  is set by the command line. If not, use default ID "0" and "1"
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param nBoardId1 is the parsed first board id from command line
* @param nBoardId2 is the parsed second board id from command line
* @return TRUE is the board id is valid
*/
BOOL ARG_GetBoardIdEX(int argc, char **argv, int nNoOfBoards, int *nBoardId1, int *nBoardId2);

/**
* Check if the ChannelID  is set by the command line. If not, use default ChannelId "0"
* Possible ChannelID range [0..7]
* @param argc program argument count
* @param argv array of program arguments
* @param nNoOfBoards is the number of detected TRION boards
* @param ChannelNo is the parsed channel id from command line
* @return TRUE is the channel id is valid
*/
BOOL ARG_GetChannelNo(int argc, char **argv, int nNoOfBoards, int *ChannelNo);

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
BOOL ARG_GetChannelNoEX(int argc, char **argv, int nNoOfBoards, int *ChannelNo1, int *ChannelNo2);

/**
* If the board given by BoardID is a board that can be used for this example.
* The comparison is only performed on the length of the entries in the list (so it can be fuzzy).
* Example: "TRION-2402-dSTG" will accept all installed connector-panel types of TRION-dSTG boards like
* TRION-2402-dSTG-8A.SB, TRION2406-dSTG-6LE, etc...
* @param nBoardID the id of the board to check the name
* @param sBoardNameNeeded list of allowed board names for the test
* @return TRUE if the board matches the list.
*/
BOOL TestBoardType( int nBoardID, const char **sBoardNameNeeded );




typedef struct ScaleInfo_tag
{
    //kx + d
    double fScaling;
    double fd; //d
    double fk; //k

    //Span - just for check reasons
    double fSpan;
} ScaleInfo;

typedef struct RangeSpan_t
{
    double rmin;
    double rmax;
} RangeSpan;


/**
* Set scale parameter from TRION API data
* Loose of precision because char -> double conversation
* @param scaleinfo pointer to the scalinfo object (== this)
* @param target
* @return TRION API error code
*/
int SetScaling(ScaleInfo* scaleinfo, const char* target);

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
int CalcScaling(ScaleInfo* scaleinfo, double minrange, double maxrange, int BitWidth);




/**
* Format the Raw Data read from API, according to the adjusted BitWidth.
* @param RawValue sample value direct from dma buffer
* @param BitWidth bit width if the sample value
* @param offset of the start bit in number of bits
* @return the converted value to signed int.
*/
signed int formatRawData( signed int RawValue, int BitWidth, int offset);


/**
* Conversion from const char* to double value
* @param Val is the string representation of the value
* @return the value as double
*/
double DblStr2Dbl( const char* Val );

/**
* Convert Boards/channels range (const char*) to double value
*
* @param  sTarget       Target to get range from (eg "BoardID0/AI7")
* @param  rangespan     will hold the parsed information (min, max)
* @return int           0 on OK
*/
int GetAdjustedRange(const char* sTarget, RangeSpan* rangespan);


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
double GetMaxARef( int nBoardID, const char *sBoardID, const char **sBoardNameNeeded, const double *maxArefVal );

/**
* Dump and pretty print the xml tree.
*/
void DumpXmlTree(const char* tree);


/**
 * Determine and list the available network interfaces.
 */
void ListNetworkInterfaces();



typedef enum
{
    TRION_CONFIG_PATH,
    TRION_LOG_PATH,
    TRION_SYSTEM_XML_PATH,
    TRION_TEST_RESULTS_PATH,
    TRION_BACKUP_PATH,
} TRION_PATH_ENUM;

/**
 * Get the configuration/log/.. path used by TRION API.
 * This is the path where the essential API configuration files
 * are located (eg dwpxi_api_config.xml).
 * @param path is a pointer to the string buffer for the path
 * @param max_path is the size of the string buffer.
 * @return TRION API error code
 */
int GetApiPath(TRION_PATH_ENUM path_code, char* path, size_t max_path);



int TRION_GetBoardName(int nBoardID, char* sBoardName, int len);

// Channels
int TRION_GetNrOfChannelsAI(int nBoardID);
int TRION_GetNrOfChannelsCNT(int nBoardID);
int TRION_GetNrOfChannelsDI(int nBoardID);
int TRION_GetNrOfChannelsBoardCNT(int nBoardID);

// Acquisition Properties
int TRION_AcqProp_GetMinSampleRate(int nBoardID);
int TRION_AcqProp_GetMaxSampleRate(int nBoardID);

int TRION_AcqProp_GetNumResolutionAI(int nBoardID);
int TRION_AcqProp_GetResolutionAI(int nBoardID, int index);

// Channel Properties
int TRION_ChanProp_GetNumModesAI(int nBoardID, int chan_index);
int TRION_ChanProp_GetModeNameAI(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len);

int TRION_ChanProp_GetNumModesCNT(int nBoardID, int chan_index);
int TRION_ChanProp_GetModeNameCNT(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len);

int TRION_ChanProp_GetNumModesDI(int nBoardID, int chan_index);
int TRION_ChanProp_GetModeNameDI(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len);

// (internal) Channel Properties
int TRION_ChanProp_GetNumModesBoardCNT(int nBoardID, int chan_index);
int TRION_ChanProp_GetModeNameBoardCNT(int nBoardID, int chan_index, int mode_index, char* sBuffer, int len);

// Generic Channel Property Accessors
int TRION_ChanProp_GetNum(int nBoardID, int chan_index, const char* ch_name, const char* mode, const char* prop);
int TRION_ChanProp_GetEntry(int nBoardID, int chan_index, const char* ch_name, const char* mode, const char* prop, int index, char* sBuffer, int len);

#endif
