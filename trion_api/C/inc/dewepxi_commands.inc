TRION_COMMAND(OPEN_BOARD,                   0x0001)  // Set
TRION_COMMAND(CLOSE_BOARD,                  0x0002)  // Set
TRION_COMMAND(RESET_BOARD,                  0x0003)  // Set
TRION_COMMAND(START_ACQUISITION,            0x0004)  // Get / Set
TRION_COMMAND(STOP_ACQUISITION,             0x0005)  // Set
TRION_COMMAND(ACT_SAMPLE_COUNT,             0x000A)  // Get      Read Register ADC_SCNT

TRION_COMMAND(UPDATE_PARAM_ALL,             0x0010)  // Set
TRION_COMMAND(UPDATE_PARAM_ACQ_ALL,         0x0011)  // Set
TRION_COMMAND(UPDATE_PARAM_CHN_ALL,         0x0012)  // Set
TRION_COMMAND(UPDATE_PARAM_ACQ,             0x0013)  // Set
TRION_COMMAND(UPDATE_PARAM_ACQ_SR,          0x0041)  // Set
TRION_COMMAND(UPDATE_PARAM_ACQ_BUFFER,      0x0042)  // Set
TRION_COMMAND(UPDATE_PARAM_MUX,             0x0014)  // Set
TRION_COMMAND(UPDATE_PARAM_INTSIG0,         0x0015)  // Set
TRION_COMMAND(UPDATE_PARAM_INTSIG1,         0x0016)  // Set
TRION_COMMAND(UPDATE_PARAM_AI,              0x0017)  // Set
TRION_COMMAND(UPDATE_PARAM_CNT,             0x0018)  // Set
TRION_COMMAND(UPDATE_PARAM_DI,              0x0019)  // Set
TRION_COMMAND(UPDATE_PARAM_BOARD_CNT,       0x001A)  // Set
TRION_COMMAND(UPDATE_PARAM_CAN,             0x001B)  // Set
TRION_COMMAND(UPDATE_PARAM_FREQOUTEXTTRIG0, 0x0036)  // Set
TRION_COMMAND(UPDATE_PARAM_FREQOUTEXTTRIG1, 0x0037)  // Set

TRION_COMMAND(UPDATE_PARAM_CANFD,           0x0023)  // Set
TRION_COMMAND(UPDATE_PARAM_UART,            0x0052)  // Set
TRION_COMMAND(UPDATE_PARAM_AO_PATTERN,      0x0053)  // Set
TRION_COMMAND(UPDATE_PARAM_AO_CONST_VALUE,  0x0054)  // Set
TRION_COMMAND(UPDATE_PARAM_AO_SUM_COEFF,    0x0055)  // Set
TRION_COMMAND(ASYNC_POLLING_TIME,           0x001C)  // Set
TRION_COMMAND(ASYNC_FRAME_SIZE,             0x001D)  // Set
TRION_COMMAND(UPDATE_PARAM_ACQ_TIMING,      0x001E)  // Set
TRION_COMMAND(TIMING_STATE,                 0x001F)  // Set

// Backwards compatibility (R)
TRION_COMMAND(BUFFER_BLOCK_SIZE,            0x0102)  // Set
TRION_COMMAND(BUFFER_BLOCK_COUNT,           0x0107)  //
TRION_COMMAND(BUFFER_START_POINTER,         0x0020)  // Get
TRION_COMMAND(BUFFER_END_POINTER,           0x0021)  // Get
TRION_COMMAND(BUFFER_TOTAL_MEM_SIZE,        0x0028)  // Get
TRION_COMMAND(BUFFER_CLEAR_ERROR,           0x0022)  // Set
TRION_COMMAND(BUFFER_AVAIL_NO_SAMPLE,       0x0025)  // Get
TRION_COMMAND(BUFFER_ACT_SAMPLE_POS,        0x0026)  // Get
TRION_COMMAND(BUFFER_FREE_NO_SAMPLE,        0x0027)  // Set
TRION_COMMAND(BUFFER_ONE_SCAN_SIZE,         0x002A)  // Get
TRION_COMMAND(BUFFER_ONE_BLOCK_SIZE,        0x0029)  // Get


// DMA0, DMA1 ... buffer access (R/W), Rule: BUFFER_i+1_ = BUFFER_i_ + offset (0x0020)
TRION_COMMAND(BUFFER_0_BLOCK_SIZE,              0x0600)  // Set / Get
TRION_COMMAND(BUFFER_0_BLOCK_COUNT,             0x0601)  // Set / Get
TRION_COMMAND(BUFFER_0_START_POINTER,           0x0602)  // Get
TRION_COMMAND(BUFFER_0_END_POINTER,             0x0603)  // Get
TRION_COMMAND(BUFFER_0_TOTAL_MEM_SIZE,          0x0604)  // Get
TRION_COMMAND(BUFFER_0_CLEAR_ERROR,             0x0605)  // Set
TRION_COMMAND(BUFFER_0_AVAIL_NO_SAMPLE,         0x0606)  // Get
TRION_COMMAND(BUFFER_0_ACT_SAMPLE_POS,          0x0607)  // Get
TRION_COMMAND(BUFFER_0_FREE_NO_SAMPLE,          0x0608)  // Set
TRION_COMMAND(BUFFER_0_ONE_SCAN_SIZE,           0x0609)  // Get
TRION_COMMAND(BUFFER_0_WRITE_SAMPLE_POS,        0x060A)  // Get
TRION_COMMAND(BUFFER_0_AVAIL_FREE_MEM,          0x060B)  // Get
TRION_COMMAND(BUFFER_0_WAIT_FREE_MEM,           0x060C)  // Get
TRION_COMMAND(BUFFER_0_COMMIT_SAMPLES,          0x060D)  // Set
TRION_COMMAND(BUFFER_0_SAMPLE_COUNT,            0x060E)  // Get
TRION_COMMAND(BUFFER_0_WAIT_AVAIL_NO_SAMPLE,    0x060F)  // Get
TRION_COMMAND(BUFFER_0_BOARD_MEM_SIZE,          0x0610)  // Get
TRION_COMMAND(BUFFER_0_FREE_BOARD_MEM_SIZE,     0x0611)  // Get
TRION_COMMAND(BUFFER_0_NUM_SAMPLES_IN_BOARD_MEM,0x0612)  // Get

TRION_COMMAND(BUFFER_1_BLOCK_SIZE,              0x0620)  // Set / Get
TRION_COMMAND(BUFFER_1_BLOCK_COUNT,             0x0621)  // Set / Get
TRION_COMMAND(BUFFER_1_START_POINTER,           0x0622)  // Get
TRION_COMMAND(BUFFER_1_END_POINTER,             0x0623)  // Get
TRION_COMMAND(BUFFER_1_TOTAL_MEM_SIZE,          0x0624)  // Get
TRION_COMMAND(BUFFER_1_CLEAR_ERROR,             0x0625)  // Set
TRION_COMMAND(BUFFER_1_AVAIL_NO_SAMPLE,         0x0626)  // Get
TRION_COMMAND(BUFFER_1_ACT_SAMPLE_POS,          0x0627)  // Get
TRION_COMMAND(BUFFER_1_FREE_NO_SAMPLE,          0x0628)  // Set
TRION_COMMAND(BUFFER_1_ONE_SCAN_SIZE,           0x0629)  // Get
TRION_COMMAND(BUFFER_1_WRITE_SAMPLE_POS,        0x062A)  // Get
TRION_COMMAND(BUFFER_1_AVAIL_FREE_MEM,          0x062B)  // Get
TRION_COMMAND(BUFFER_1_WAIT_FREE_MEM,           0x062C)  // Get
TRION_COMMAND(BUFFER_1_COMMIT_SAMPLES,          0x062D)  // Set
TRION_COMMAND(BUFFER_1_SAMPLE_COUNT,            0x062E)  // Get
TRION_COMMAND(BUFFER_1_WAIT_AVAIL_NO_SAMPLE,    0x062F)  // Get
TRION_COMMAND(BUFFER_1_BOARD_MEM_SIZE,          0x0630)  // Get
TRION_COMMAND(BUFFER_1_FREE_BOARD_MEM_SIZE,     0x0631)  // Get
TRION_COMMAND(BUFFER_1_NUM_SAMPLES_IN_BOARD_MEM,0x0632)  // Get


TRION_COMMAND(GET_UART_STATUS,              0x0030)  // Get
TRION_COMMAND(BUFFER_WAIT_AVAIL_NO_SAMPLE,  0x0031)  // Get
TRION_COMMAND(ACQ_STATE,                    0x0032)  // Get
TRION_COMMAND(UPDATE_PARAM_AREF,            0x0033)  // Set
TRION_COMMAND(TIMING_TIME,                  0x0034)  // Get
TRION_COMMAND(GPS_RECEIVER_RESET,           0x0035)  //Set

TRION_COMMAND(PXI_LINE_STATE,               0x0040)  // Get

TRION_COMMAND(DISCRET_STATE_SET,            0x0050)  // Set
TRION_COMMAND(DISCRET_STATE_CLEAR,          0x0051)  // Set

TRION_COMMAND(DISCRET_GROUP32_SET,          0x0060)  // Set (will set all 32 Bits of a grou, will have encoded groupinfo in lowernibble ( x60, x61, x62,...)

TRION_COMMAND(IDLED_BOARD_ON,               0x0070)  // Set
TRION_COMMAND(IDLED_BOARD_OFF,              0x0071)  // Set
TRION_COMMAND(IDLED_CHANNEL_ON,             0x0072)  // Set
TRION_COMMAND(IDLED_CHANNEL_OFF,            0x0073)  // Set



TRION_COMMAND(GETDATA_TIME_OUT,                     0x0A01)  // Get
TRION_COMMAND(UPDATE_PARAM_ACQ_ROUTE,               0x0103)  // Set
TRION_COMMAND(INT_REF_VAL,                          0x0201)  // Set / Get
TRION_COMMAND(REF_MODE,                             0x0202)  // Set / Get
TRION_COMMAND(UPDATE_PARAM_INTCAL,                  0x0205)  // Set
TRION_COMMAND(BOARD_ACT_SAMPLE_COUNT,               0x0301)  // R        Read Register ADC_SCNT TBD: 64Bit command
TRION_COMMAND(BOARD_ADC_DELAY,                      0x0302)  // R        Obtain ADC-Delay (valid only AFTER setting a samplerate)
TRION_COMMAND(BOARD_AFSPAN,                         0x0303)  // R        Query %of Samplerate that is alias free
TRION_COMMAND(BOARD_RESET_SELFCAL,                  0x0305)  // W        Reset the whole Selfcal-Section in BaseE2Prom with the Default-values
TRION_COMMAND(BOARD_BASEEEPROM_WRITE,               0x0401)  // W        Write the BaseEEprom-File to EEProm
TRION_COMMAND(BOARD_CONEEPROM_WRITE,                0x0402)  // W        Write the ConnectorEEprom-File to EEProm
TRION_COMMAND(BOARD_BASEEEPROM_SELFCAL_WRITE,       0x0403)  // W        Write the BaseEEprom-File to EEProm
TRION_COMMAND(BOARD_BASEEEPROM_RESTORE_BACKUP,      0x0404)  // W        Try to restore a backup for base e2prom
TRION_COMMAND(BOARD_CONEEPROM_RESTORE_BACKUP,       0x0405)  // W        Try to restore a backup for con e2prom
TRION_COMMAND(BOARD_AITEDSEX_READ,                  0x0406)  // R        Reads the AiTEDSEx data from the TEDS chip
TRION_COMMAND(BOARD_AITEDSEX_WRITE,                 0x0407)  // W        Write the AiTEDSEx data back to the TEDS chip
TRION_COMMAND(BOARD_AITEDSEX_SYNCHRONIZE,           0x0408)  // U        Synchronizes raw TEDS memory data with updated TEDSInfo data
TRION_COMMAND(BOARD_FW_WRITE_PROTECT,               0x0409)  // W
TRION_COMMAND(BOARD_AITEDSEX_READ_CHAIN,            0x040A)  // R        Reads the AiTEDSEx data from a chain of TEDS chips

// Peripheral
TRION_COMMAND(BOARD_PERIPHERAL_EEPROM_WRITE,        0x350)  // W        Write the Peripheral-File to EEProm

// Sync Data Samples readout
TRION_COMMAND(BOARD_ACT_SAMPLE_VALUE_COUNT,         0x0360)  // Get      Read the number of 32bit values in the SYNC_DATA_SAMPLES register (returns 0 if not supported)
TRION_COMMAND(BOARD_ACT_SAMPLE_VALUE_POINTER,       0x0361)  // Get      Updates all used SYNC_DATA_SAMPLES registers and returns the address of a buffer with BOARD_ACT_SAMPLE_VALUE_COUNT 32bit values

TRION_COMMAND(BOARD_NEW_SAMPLE_CALLBACK,            0x0362)  // Set/Get  Registers/Clears a function "void fn(int board_no, void* context)" that will be called when a new sample is available (sets NEW_SAMPLES_IRQ_CTRL accordingly)
TRION_COMMAND(BOARD_NEW_SAMPLE_CALLBACK_CONTEXT,    0x0363)  // Set/Get  Read/Write the context pointer that is passed to the BOARD_NEW_SAMPLE_CALLBACK function each call


TRION_COMMAND(BOARD_REINIT,					0x0420)  // Set

TRION_COMMAND(OPEN_BOARD_ALL,               0x0450)  // Set
TRION_COMMAND(CLOSE_BOARD_ALL,              0x0451)  // Set
TRION_COMMAND(RESET_BOARD_ALL,              0x0452)  // Set

TRION_COMMAND(CAN_OPEN,                     0x0460)  // Set
TRION_COMMAND(CAN_CLOSE,                    0x0461)  // Set
TRION_COMMAND(CAN_START,                    0x0462)  // Set
TRION_COMMAND(CAN_STOP,                     0x0463)  // Set

TRION_COMMAND(UART_OPEN,                    0x0470)  // Set
TRION_COMMAND(UART_CLOSE,                   0x0471)  // Set
TRION_COMMAND(UART_START,                   0x0472)  // Set
TRION_COMMAND(UART_STOP,                    0x0473)  // Set

TRION_COMMAND(RESCAN_CHANNEL_MODULES,       0x0483)  // Set

//Strict Test Commands
TRION_COMMAND(BOARD_BASEEEPROM_DESTROY,     0x0499)  // W        Corrupt the BaseE2Prom
