#
# Project DEWETRON TRION SDK - misc examples
#

add_executable(Board_ID_LED
  Board_ID_LED/board_id_led.c
  )
SampleBuildSettings(Board_ID_LED "misc")

add_executable(ListBoards
ListBoards/list_boards.c
  )
SampleBuildSettings(ListBoards "misc")
