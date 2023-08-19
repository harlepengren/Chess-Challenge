# Testpositions
from .board import Board
from .search import Search

def InitTest():
    board = Board()
    search = Search(board)
    print(search.GetMove('d4','w'))

def GetTestString():
    return '7B/1R6/8/8/8/8/1p6/8'