#! /bin/env python3
# Copyright DEWETRON GmbH 2019

import sys
sys.path.append('../../../trion_api/python')

# Import the core and GUI elements of Qt
from PySide2.QtCore import Qt, QPointF, QTimer
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
        self.is_api_loaded = False
        self.chart = QtCharts.QChart()
        self.chart.setAnimationOptions(QtCharts.QChart.NoAnimation)

        self.setupGUI()


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
                self.selectAPI("TRION")
            elif self.api_trionet_api.isChecked():
                self.selectAPI("TRIONET")

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

    def showStatus(self, text, style = "color:black"):
        """
        show text in status bar
        """
        self.statuslabel.setText(text)
        self.statuslabel.setStyleSheet(style)

    def selectAPI(self, api_name):
        """
        Select and load TRION or TRIONET api.
        """
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


    def initTrion(self):
        """
        Initialize TRION (or TRIONET)
        """
        [nErrorCode, nNoOfBoards] = DeWeDriverInit()
        if abs(nNoOfBoards) == 0:
            self.showStatus("No Trion cards found")
        elif nNoOfBoards < 0:
            self.showStatus("%d Trion cards found (Simulation)" % abs(nNoOfBoards))
        else:
            self.showStatus("%d Trion cards found" % nNoOfBoards)

        self.cb_trion_board.clear()
        self.cb_channel.clear()

        num_boards = abs(nNoOfBoards)

        if num_boards > 0:
            nErrorCode = DeWeSetParam_i32(0, CMD_OPEN_BOARD_ALL, 0)
            nErrorCode = DeWeSetParam_i32(0, CMD_RESET_BOARD_ALL, 0)

            for i in range(num_boards):
                [nErrorCode, board_name] = DeWeGetParamStruct_str("BoardID%d" % i, "BoardName")
                if len(board_name) == 0:
                    board_name = "Unknown board"
                self.cb_trion_board.addItem("%d: %s " % ( i, board_name))
                [nErrorCode, board_prop_xml] = DeWeGetParamStruct_str("BoardID%d" % i, "BoardProperties")

                prop_doc = et.fromstring(board_prop_xml)
                elem_list = prop_doc.findall("ChannelProperties/*")
                for elem in elem_list:
                    if elem.tag != "XMLVersion":
                        self.cb_channel.addItem(elem.tag)


        


if __name__ == "__main__":
    app = QApplication(sys.argv)

    widget = MainDialog()
    widget.show()
    sys.exit(app.exec_())
