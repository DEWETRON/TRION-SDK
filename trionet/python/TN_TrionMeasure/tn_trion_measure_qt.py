#! /bin/env python3
# Copyright DEWETRON GmbH 2019

import sys

# Import the core and GUI elements of Qt
from PySide2.QtCore import Qt, QPointF, QTimer
from PySide2 import QtGui
from PySide2.QtWidgets import *
from PySide2.QtCharts import *


class MainDialog(QWidget):
    """
    Sample main window
    """
    def __init__(self, parent=None):
        super(MainDialog, self).__init__(parent)

        self.setupGUI()

    def setupGUI(self):
        self.setWindowTitle("TRION Measure qt")

        # self.statusbar = QStatusBar(self)
        # self.statuslabel = QLabel("Status", self)
        # self.statuslabel.setFrameStyle(QFrame.Panel | QFrame.Sunken)
        # self.statusbar.addPermanentWidget(self.statuslabel, 1)

if __name__ == "__main__":
    app = QApplication(sys.argv)

    widget = MainDialog()
    widget.show()
    sys.exit(app.exec_())
