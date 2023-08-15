from .game import Replay
import json
import random

class MoveExporter:
    def ProcessGame(self, filename):
        currentGame = Replay()
        currentGame.ReadFile(filename)

        numMoves = currentGame.MoveCount()
        randomMove = random.randint(0,numMoves-1)
        currentGame.NextMove(randomMove)

        # Create the json entry
        gameDict = {}
        # Determine the current player, this assumes we start with white
        # a better option would be to implement tracking in the board
        player = "w" if (randomMove % 2) == 0 else "b"
        gameDict["FEN"] = currentGame.board.ExportFEN(player)
        gameDict["totalMoves"] = numMoves
        gameDict["move"] = randomMove

        if(currentGame.winner != None):
            gameDict["winner"] = currentGame.winner

        return gameDict

    def LoadGames(self, directory):
    '''Loads all games in a directory, processes them, and exports a json file.'''
        # Check whether the directory is valid
        # for each file in directory
        # process game and add game to list

        # save the list as a JSON file

        pass
