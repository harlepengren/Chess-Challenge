from .game import Replay
import json
import random
import os
import pathlib

class MoveExporter:
    def ProcessGame(self, filename):
        currentGame = Replay()
        gameDict = {}

        try: 
            currentGame.ReadFile(filename)

            numMoves = currentGame.MoveCount()

            if (numMoves -1) > 2:
                randomMove = random.randint(2,numMoves-1)
                currentGame.NextMove(randomMove)

                # Create the json entry
                # Determine the current player, this assumes we start with white
                # a better option would be to implement tracking in the board
                player = "w" if (randomMove % 2) == 0 else "b"
                gameDict["tags"] = currentGame._tags
                gameDict["FEN"] = currentGame.board.ExportFEN(player)
                gameDict["totalMoves"] = numMoves
                gameDict["move"] = randomMove

                if(currentGame.winner != None):
                    gameDict["winner"] = currentGame.winner
        except Exception as e:
            print("Error: " + filename)
            print(e)

        return gameDict

    def LoadGames(self, directory, outfile):
        '''Loads all games in a directory, processes them, and exports a json file.'''

        gameList = []

        # Check whether the directory is valid
        if(not os.path.exists(directory) or not os.path.isdir(directory)):
            print("The directory either doesn't exist or is not a directory.")
            return

        # for each file in directory, process the game and add to the list
        for currentFile in os.listdir(directory):
            if os.path.isfile(directory + '/' + currentFile) and pathlib.Path(currentFile).suffix == '.pgn':
                newGame = self.ProcessGame(directory + '/' + currentFile)
                if newGame != {}:
                    gameList.append(newGame)

        # save the list as a JSON file
        jsonString = json.dumps(gameList,indent=4)

        with open(outfile, "w") as out:
            out.write(jsonString)