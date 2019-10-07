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


class MainDialog(QWidget):
    """
    Sample main window
    """
    def __init__(self, parent=None):
        super(MainDialog, self).__init__(parent)
        self.chart = QtCharts.QChart()
        self.chart.setAnimationOptions(QtCharts.QChart.NoAnimation)

        self.setupGUI()

        self.initTrion()

    def setupGUI(self):
        self.setWindowTitle("TRION Measure qt")

        self.groupbox_api_selection = QGroupBox("&Select API", self)
        self.api_trion_api = QRadioButton("&TRION", self)
        self.api_trion_api.setChecked(True)
        self.api_trionet_api = QRadioButton("&TRIONet", self)

        layout = QHBoxLayout()
        layout.addWidget(self.api_trion_api)
        layout.addWidget(self.api_trionet_api)

        self.groupbox_api_selection.setLayout(layout)

        self.statusbar = QStatusBar(self)
        self.statuslabel = QLabel("Status", self)
        self.statuslabel.setFrameStyle(QFrame.Panel | QFrame.Sunken)
        self.statusbar.addPermanentWidget(self.statuslabel, 1)

        groupbox_chart = QGroupBox("Channel", self)
        self.chart_view = QtCharts.QChartView(self.chart)
        self.chart_view.setRenderHint(QtGui.QPainter.Antialiasing)
        self.chart_view.setMinimumSize(400, 200)

        layout = QVBoxLayout()
        layout.addWidget(self.chart_view)
        groupbox_chart.setLayout(layout)

        main_layout = QVBoxLayout()
        main_layout.addWidget(self.groupbox_api_selection)
        main_layout.addWidget(groupbox_chart)
        main_layout.addWidget(self.statusbar)
        self.setLayout(main_layout)

    def showStatus(self, text, style = "color:black"):
        """
        show text in status bar
        """
        self.statuslabel.setText(text)
        self.statuslabel.setStyleSheet(style)

    def initTrion(self):
        """
        """
        # if not DeWePxiLoad("TRIONET"):
        #     self.showStatus("dwpxi_api.dll could not be found. Exiting...")
        #     return

        if not DeWePxiLoad("TRION"):
            self.showStatus("dwpxi_api.dll could not be found. Exiting...")
            return


        [nErrorCode, nNoOfBoards] = DeWeDriverInit()
        if abs(nNoOfBoards) == 0:
            self.showStatus("No Trion cards found")
        else:
            self.showStatus("%d Trion cards found" % nNoOfBoards)


if __name__ == "__main__":
    app = QApplication(sys.argv)

    widget = MainDialog()
    widget.show()
    sys.exit(app.exec_())
