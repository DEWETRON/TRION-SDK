#! /bin/env python3
# Copyright DEWETRON GmbH 2019

import sys
import time
sys.path.append('../../../trion_api/python')

# Import the core and GUI elements of Qt
from PySide2.QtCore import Qt, QObject, QPointF, QTimer, Slot, Signal, QThread
from PySide2 import QtGui
from PySide2.QtWidgets import *
from PySide2.QtCharts import *

from dewepxi_load import *
from dewepxi_apicore import *

from xml.etree import ElementTree as et



class MainDialog(QWidget):
    """
    Sample main window
    """
    def __init__(self, parent=None):
        super(MainDialog, self).__init__(parent)
        self.chart = QtCharts.QChart()
        self.chart.setAnimationOptions(QtCharts.QChart.NoAnimation)

        self.worker = TrionMeasurementWorker(self)
        self.worker.signal_show_message.connect(self.showStatus, Qt.QueuedConnection)
        self.worker.add_channel_data.connect(self.addChannelData, Qt.QueuedConnection)

        self.chart_series = dict()

        self.setupGUI()
        self.redrawChart()


    def setupGUI(self):
        self.setWindowTitle("TRION Measure qt")

        self.groupbox_api_selection = QGroupBox("&Select API", self)
        self.api_trion_api = QRadioButton("&TRION", self)
        self.api_trionet_api = QRadioButton("&TRIONet", self)

        layout = QHBoxLayout()
        layout.addWidget(self.api_trion_api)
        layout.addWidget(self.api_trionet_api)
        self.groupbox_api_selection.setLayout(layout)


        self.groupbox_board_selection = QGroupBox("&Select Board", self)
        self.cb_trion_board = QComboBox()
        layout = QVBoxLayout()
        layout.addWidget(self.cb_trion_board)
        self.groupbox_board_selection.setLayout(layout)


        self.groupbox_channel_selection = QGroupBox("&Select Channel", self)
        self.cb_channel = QComboBox()
        layout = QVBoxLayout()
        layout.addWidget(self.cb_channel)
        self.groupbox_channel_selection.setLayout(layout)

        self.groupbox_channel_config = QGroupBox("&Channel Config", self)
        self.cb_range = QComboBox()
        self.cb_sample_rate = QComboBox()
        layout = QHBoxLayout()
        layout.addWidget(self.cb_range)
        layout.addWidget(self.cb_sample_rate)
        self.groupbox_channel_config.setLayout(layout)


        self.statusbar = QStatusBar(self)
        self.statuslabel = QLabel("Status", self)
        self.statuslabel.setFrameStyle(QFrame.Panel | QFrame.Sunken)
        self.statusbar.addPermanentWidget(self.statuslabel, 1)

        groupbox_chart = QGroupBox("Channel Data", self)
        self.chart_view = QtCharts.QChartView(self.chart)
        self.chart_view.setRenderHint(QtGui.QPainter.Antialiasing)
        self.chart_view.setMinimumSize(400, 200)

        layout = QVBoxLayout()
        layout.addWidget(self.chart_view)
        groupbox_chart.setLayout(layout)

        def onApiChanged():
            if self.api_trion_api.isChecked():
                self.worker.selectAPI("TRION")
            elif self.api_trionet_api.isChecked():
                self.worker.selectAPI("TRIONET")

        self.api_trion_api.toggled.connect(onApiChanged)
        self.api_trion_api.setChecked(True)

        main_layout = QVBoxLayout()
        main_layout.addWidget(self.groupbox_api_selection)
        main_layout.addWidget(self.groupbox_board_selection)
        main_layout.addWidget(self.groupbox_channel_selection)
        main_layout.addWidget(self.groupbox_channel_config)
        main_layout.addWidget(groupbox_chart)
        main_layout.addWidget(self.statusbar)
        self.setLayout(main_layout)

    
    @Slot(str, str)
    def showStatus(self, text, style = "color:black"):
        """
        show text in status bar
        """
        self.statuslabel.setText(text)
        self.statuslabel.setStyleSheet(style)


    def initChart(self):
        self.chart.removeAllSeries()
        for axis in self.chart.axes(Qt.Horizontal): self.chart.removeAxis(axis)
        for axis in self.chart.axes(Qt.Vertical): self.chart.removeAxis(axis)


    def redrawChart(self):
        self.initChart()


    def addChannelData(self, channel_data_list):
        """
        Add new sample block
        """
        self.chart.removeAllSeries()
        series = QtCharts.QLineSeries()
        series.append(channel_data_list)
        self.chart.addSeries(series)


class TrionMeasurementWorker(QThread):
    """
    Measurement worker thread
    """
    signal_show_message = Signal(str, str)
    add_channel_data = Signal(list)

    def __init__(self, parent=None):
        """
        constructor
        """
        QThread.__init__(self, parent)
        self.gui = parent
        self.exiting = False
        self.is_api_loaded = False
        self.board_id = 0

    def run(self):
        """
        ACQ loop
        """

        self.configureChannel()
        self.configureAcquisition()

        nReadPos      = 0
        nAvailSamples = 0
        nRawData      = 0
        sample_index  = 0

        # Get detailed information about the ring buffer
        # to be able to handle the wrap around
        [nErrorCode, nBufEndPos] = DeWeGetParam_i64( self.board_id, CMD_BUFFER_END_POINTER)
        [nErrorCode, nBufSize]   = DeWeGetParam_i32( self.board_id, CMD_BUFFER_TOTAL_MEM_SIZE)

        nErrorCode = DeWeSetParam_i32( self.board_id, CMD_START_ACQUISITION, 0)
        while self.exiting==False:
            # Get the number of samples already stored in the ring buffer
            [nErrorCode, nAvailSamples] = DeWeGetParam_i32( self.board_id, CMD_BUFFER_AVAIL_NO_SAMPLE)

            if nAvailSamples > 0:
                # Get the current read pointer
                [nErrorCode, nReadPos] = DeWeGetParam_i64( self.board_id, CMD_BUFFER_ACT_SAMPLE_POS)

                channel_data = []

                # Read the current samples from the ring buffer
                for i in range(0, nAvailSamples):
                    # Get the sample value at the read pointer of the ring buffer
                    nRawData = DeWeGetSampleData(nReadPos)

                    # Print the sample value
                    # print(nRawData)
                    # sys.stdout.flush()

                    channel_data.append(QPointF(sample_index, nRawData))
                    sample_index += 1

                    # Increment the read pointer
                    nReadPos = nReadPos + 4
                    # Handle the ring buffer wrap around
                    if nReadPos > nBufEndPos:
                        nReadPos -= nBufSize
                    # Free the ring buffer after read of all values

                nErrorCode = DeWeSetParam_i32( self.board_id, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples)

                self.addChannelData(channel_data)

                # wait for 100ms
                time.sleep(0.1)

        nErrorCode = DeWeSetParam_i32( self.board_id, CMD_STOP_ACQUISITION, 0)

    def startWorker(self):
        """
        Start worker thread
        """
        if not self.isRunning():
            self.start()

    def stopWorker(self):
        """
        Stop worker thread
        """
        if self.isRunning():
            self.exiting = True
            self.terminate()

    def selectAPI(self, api_name):
        """
        Select and load TRION or TRIONET api.
        """

        self.stopWorker()

        if self.is_api_loaded:
            DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0)
            DeWeDriverDeInit()
        
        DeWePxiUnload()

        if not DeWePxiLoad(api_name):
            if api_name == "TRION":
                self.showStatus("dwpxi_api.dll could not be found.")
            if api_name == "TRIONET":
                self.showStatus("dwpxi_netapi.dll could not be found.")
            return

        self.is_api_loaded = True
        self.api_backend_name = api_name
        self.initTrion()

        self.startWorker()


    def initTrion(self):
        """
        Initialize TRION (or TRIONET)       
        """
        if self.isRunning():
            self.showStatus("initTrion not possible with active worker thread")
            return

        [nErrorCode, nNoOfBoards] = DeWeDriverInit()
        if abs(nNoOfBoards) == 0:
            self.showStatus("No Trion cards found")
        elif nNoOfBoards < 0:
            self.showStatus("%d Trion cards found (Simulation)" % abs(nNoOfBoards))
        else:
            self.showStatus("%d Trion cards found" % nNoOfBoards)

        self.gui.cb_trion_board.clear()
        self.gui.cb_channel.clear()

        num_boards = abs(nNoOfBoards)

        if num_boards > 0:
            nErrorCode = DeWeSetParam_i32(0, CMD_OPEN_BOARD_ALL, 0)
            nErrorCode = DeWeSetParam_i32(0, CMD_RESET_BOARD_ALL, 0)

            for i in range(num_boards):
                [nErrorCode, board_name] = DeWeGetParamStruct_str("BoardID%d" % i, "BoardName")
                if len(board_name) == 0:
                    board_name = "Unknown board"
                self.gui.cb_trion_board.addItem("%d: %s " % ( i, board_name))
                [nErrorCode, board_prop_xml] = DeWeGetParamStruct_str("BoardID%d" % i, "BoardProperties")

                prop_doc = et.fromstring(board_prop_xml)
                elem_list = prop_doc.findall("ChannelProperties/*")
                for elem in elem_list:
                    if elem.tag != "XMLVersion":
                        # add channel names
                        self.gui.cb_channel.addItem(elem.tag)


    def configureAcquisition(self):
        """
        configure Acquisition setup
        """
        # Set configuration to use one board in standalone operation
        target = "BoardID%d/AcqProp" % self.board_id
        nErrorCode = DeWeSetParamStruct_str( target, "OperationMode", "Slave")
        nErrorCode = DeWeSetParamStruct_str( target, "ExtTrigger", "False")
        nErrorCode = DeWeSetParamStruct_str( target, "ExtClk", "False")

        nErrorCode = DeWeSetParam_i32(self.board_id, CMD_BUFFER_BLOCK_SIZE, 200)
        nErrorCode = DeWeSetParam_i32(self.board_id, CMD_BUFFER_BLOCK_COUNT, 50)
        nErrorCode = DeWeSetParam_i32(self.board_id, CMD_UPDATE_PARAM_ALL, 0)


    def configureChannel(self):
        """
        configureChannel
        (has to be called before configureAcquisition)
        """
        nErrorCode = DeWeSetParamStruct_str( "BoardID0/AIAll", "Used", "False")
        nErrorCode = DeWeSetParamStruct_str( "BoardID0/AI0", "Used", "True")


    def showStatus(self, text, style = "color:black"):
        """
        show text in status bar
        """
        self.signal_show_message.emit(text, style)

    def addChannelData(self, channel_data):
        """
        add samples to graph
        """
        self.add_channel_data.emit(channel_data)


if __name__ == "__main__":
    app = QApplication(sys.argv)
    widget = MainDialog()
    widget.show()
    ret = app.exec_()
    widget.worker.stopWorker()
    sys.exit(ret)
