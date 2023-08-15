import re
import random
import copy
from .board import Board
from .search import Search

# TO DO: Move the player switching from Board into this class.
class Game:
    """Class that allows players to play a text based games."""
    def __init__(self, numPlayers, side="w"):
        """Set up the game. Input: # of players and side of human for Player v Computer games."""
        self._numPlayers = numPlayers
        self._board = Board()
        self._currentSide = 'w'
        #self._AI = ChessAI(self._board)

        if numPlayers == 0:
            self._players = {'w': False, 'b': False}
        elif numPlayers == 2:
            self._players = {'w': True, 'b': True}
        elif numPlayers ==1 and side == 'w':
            self._players = {'w': True, 'b': False}
        else:
            self._players = {'w': False, 'b': True}
        
    def PlayGame(self):
        """Play the game."""
        self._board.PrintBoard()

        while not self._board.Checkmate(self._currentSide) and not next(self.MaxMoves()):
            if self._players[self._currentSide]:
                move = input("Next Move: ")
                if move == 'q':
                    break
                
                # TO DO: Check whether the move is valid
                result = self._board.Move(move,self._currentSide)
            #else:
            #    move = self._AI.NextAIMove(self._currentSide)
                
                if move == -1:
                    break

                #print(move)
                result = self._board._Move(move[0][1],move[1])
                print(move, result)

            if result:
                self._board.PrintBoard()
                self._NextPlayer()
                if self._board.IsCheck(self._currentSide):
                    if self._board.Checkmate(self._currentSide):
                        print("Checkmate")
                    else:
                        print("Check")
                print("Next Player: ", self._currentSide)

    def _NextPlayer(self):
        """Switch the current player."""
        if self._currentSide == 'w':
            self._currentSide = 'b'
        else:
            self._currentSide = 'w'

    def MaxMoves(self):
        """Check whether it is greater than 30 moves."""
        moves = 0
        while moves < 30:
            yield False
            moves += 1
        
        return True

class Replay:
    """Class allows you to input a game and walk through the moves of the game."""
    
    def __init__(self, fileName=None, analysis=False):
        self.board = Board()
        self.search = Search(self.board)
        self._tags = {}
        self._moves = []
        self._boardPositions = []
        self.currentMove = 0

        if fileName != None:
            self.ReadFile(fileName)
            #self.CreateBoardPositions()

        #self._analysis = analysis
        #if analysis:
        #    self._ai = AI(self.board)

    def ReadFile(self, fileName):
        gameFile = open(fileName,"r")
        # For each line in the file
        for line in gameFile:
            self.ParseLine(line)
        
        gameFile.close

        # The last item in the moves list should specify who won (i.e., 1-0, 0-1, 1/2-1/2). Remove that from the moves list
        if len(self._moves) > 0 and (re.match('[0-9](/2)?-[0-9]',self._moves[len(self._moves)-1]) != None):
            gameResult = self._moves[len(self._moves)-1]
            if(gameResult == '1-0'):
                self.winner = 'w'
            elif (gameResult == '0-1'):
                self.winner = 'b'
            else:
                self.winner = 'draw'

            self._moves = self._moves[:-1]
        else:
            self.winner = None

        if len(self._moves) > 0:
            self.CreateBoardPositions()
    
    def ParseLine(self, line):
        # Does this line have moves or is it a tag pair
        if re.search('\[.*\]',line):
            # Tag Pair
            self.ProcessTagPair(line)
        else:
            self.ProcessMoveLine(line)
            pass
    
    def ProcessTagPair(self, line):
        # Remove the brackets
        currentLine = line[1:][:len(line)-3]
        split = re.search('\s',currentLine)
        self._tags[currentLine[:split.span()[0]]] = currentLine[split.span()[1]:]

    def ProcessMoveLine(self, line):
        moves = re.split('\s',line)
        
        for move in moves:
            if (re.search('[0-9]+\.',move) == None) and (move != ''):
                self._moves.append(move)

    def CreateBoardPositions(self):
        self._boardPositions = []

        self._boardPositions.append(self.board.ExportFEN('w'))
        
        counter = 0
        for currMove in self._moves:
            if counter % 2 == 0:
                color = 'w'
            else:
                color = 'b'

            self._boardPositions.append(self.ExecuteMove(currMove, color))
            counter += 1
        
        print("Processed " + str(counter) + " moves.")
        self.board.SetFEN(self._boardPositions[0])
        
    def ExecuteMove(self, move, player):
        start, end, special = self.search.GetMove(move,player)
        self.board.MovePiece(start,end,special)
        return self.board.ExportFEN(player)

    def NextMove(self,num=1):
        for _ in range(num):
            print(self._moves[self.currentMove])
            self.currentMove += 1

        self.board.SetFEN(self._boardPositions[self.currentMove])
        print(self.board)

    def PreviousMove(self,num=1):
        self.currentMove -= num
    
        self.board.SetFEN(self._boardPositions[self.currentMove])
        print(self.board)

    def MoveCount(self):
        return len(self._moves)

