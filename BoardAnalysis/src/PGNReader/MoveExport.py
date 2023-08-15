from .game import Replay
import json
import random

class MoveExporter:
    def ProcessGame(self, filename):
        currentGame = Replay(filename)

        numMoves = currentGame.MoveCount()
        randomMove = random.randint(0,numMoves-1)
        currentGame.NextMove(randomMove)

        # Create the json entry
        gameDict = {}
        # Determine the current player, this assumes we start with white
        # a better option would be to implement tracking in the board
        player = "w" if (randomMove % 2) == 0 else "b"
        gameDict["FEN"] = currentGame.board.Export("FEN",player)
        gameDict["totalMoves"] = numMoves
        gameDict["move"] = randomMove

        if(currentGame.winner != None):
            gameDict["winner"] = currentGame.winner

        print(gameDict)
