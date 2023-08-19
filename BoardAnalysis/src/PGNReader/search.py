import re
#from move import *

class Search:
    """Class to search for legal moves. Each method takes a position tuple with file and rank and returns a list of potential moves."""
    allFiles = ['a','b','c','d','e','f','g','h']

    def __init__(self,board):
        self._board = board

    def _ValidRange(self,file,rank):
        if file in range(8) and rank in range(1,9):
            return True

        return False

    def _GeneralSearch(self,position,searchDirection):
        possibleMoves = []

        color = self._board.GetColor(position)

        file = self.allFiles.index(position[0])
        rank = position[1]

        for file_direction, rank_direction in searchDirection:
            currentFile = file+file_direction
            currentRank = rank+rank_direction
            while currentRank > 0 and currentRank <=8 and \
                currentFile >= 0 and currentFile < 8 and \
                self._board.GetPiece((self.allFiles[currentFile],currentRank))== None:
                
                possibleMoves.append((self.allFiles[currentFile],currentRank))
                currentFile += file_direction
                currentRank += rank_direction
            
            if self._ValidRange(currentFile, currentRank) and self._board.GetColor((self.allFiles[currentFile],currentRank))!=color:
                possibleMoves.append((self.allFiles[currentFile],currentRank))
        
        return possibleMoves

    def SearchPawn(self,position,captureOnly=False):
        """Pawn has three possible: move 1 space, move 2 spaces (if in starting position), and capture (left, right, en passant)."""
        possibleMoves = []

        file = self.allFiles.index(position[0])
        rank = position[1]

        color = self._board.GetColor(position)
        if color == 'b':
            direction = -1
        else:
            direction = 1

        testRank = rank+direction

        if not captureOnly:
            # Pawn can move 1 forward unless another piece is in front

            if self._board.GetPiece((file,testRank)) == None:
                possibleMoves.append((self.allFiles[file],testRank))

                # If in starting position, can you move one more?
                if ((color == 'b') and (rank == 7)) or ((color == 'w') and (rank == 2)):
                    if self._board.GetPiece((file,testRank+(1*direction))) == None:
                        possibleMoves.append((self.allFiles[file],testRank+1*direction))

        # Capture left and right
        if file - 1 >= 0:
            leftPiece = self._board.GetPiece((self.allFiles[file-1],testRank))
            if (leftPiece != None) and (self._board.GetPiece((self.allFiles[file-1],testRank)) != color):
                possibleMoves.append((self.allFiles[file-1],testRank))

        if file + 1 < 8:
            rightPiece = self._board.GetPiece((self.allFiles[file+1],testRank))
            if (rightPiece != None) and (self._board.GetPiece((self.allFiles[file+1],testRank)) != color):
                possibleMoves.append((self.allFiles[file+1],testRank))

        return possibleMoves

    def SearchBishop(self, position):
        return self._GeneralSearch(position,[(1,1),(-1,-1),(1,-1),(-1,1)])

    def SearchRook(self,position):
        return self._GeneralSearch(position,[(1,0),(-1,0),(0,-1),(0,1)])

    def SearchQueen(self,position):
        return self._GeneralSearch(position,[(1,0),(-1,0),(0,-1),(0,1), (1,1),(-1,-1),(1,-1),(-1,1)])

    def SearchKnight(self, position):
        possibleMoves = []
        color = self._board.GetColor(position)

        file = self.allFiles.index(position[0])
        rank = position[1]

        direction = [(-2,-1),(-2,1),(-1,2),(1,2),(2,1),(2,-1),(-1,-2),(1,-2)]

        for file_dir, rank_dir in direction:
            currFile = file+file_dir
            currRank = rank+rank_dir

            if self._ValidRange(currFile,currRank):
                currentPiece = self._board.GetPiece((self.allFiles[currFile],currRank))

                if currentPiece == None or self._board.GetColor((self.allFiles[currFile],currRank)) != color:
                    possibleMoves.append((self.allFiles[currFile],currRank))

        return possibleMoves

    def SearchKing(self, position):
            possibleMoves = []
            color = self._board.GetColor(position)

            file = self.allFiles.index(position[0])
            rank = position[1]

            direction = [(-1,-1),(-1,1),(1,-1),(1,1),(1,0),(-1,0),(0,1),(0,-1)]

            for file_dir, rank_dir in direction:
                currFile = file+file_dir
                currRank = rank+rank_dir

                if self._ValidRange(currFile,currRank):
                    currentPiece = self._board.GetPiece((self.allFiles[currFile],currRank))

                    if currentPiece == None or self._board.GetColor((self.allFiles[currFile],currRank)) != color:
                        possibleMoves.append((self.allFiles[currFile],currRank))

            # TO DO: Can't move into a check

            return possibleMoves

    def Check(self, position, color):
        """Checks whether a given set of positions is in danger of a check."""
        if color == 'w':
            opponent = 'b'
        else:
            opponent = 'w'

        # Get all opponent pieces
        opponentPieces = self._board.GetAllPieces(opponent)

        # Get potential capture moves for all opponent pieces
        for piece in opponentPieces:
            possibleMoves = []
            pieceType = str.upper(self._board.GetPiece(piece))

            if pieceType == 'P':
                possibleMoves += self.SearchPawn(piece,captureOnly=True)
            elif pieceType == 'R':
                possibleMoves += self.SearchRook(piece)
            elif pieceType == 'N':
                possibleMoves += self.SearchNight(piece)
            elif pieceType == 'B':
                possibleMoves += self.SearchBishop(piece)
            elif pieceType == 'Q':
                possibleMoves += self.SearchQueen(piece)
            elif pieceType == 'K':
                possibleMoves += self.SearchKing(piece)

            # Determine whether position is in any of those capture moves
            if position in possibleMoves:
                return True
            
        return False

    def SearchPiece(self,pieceType,position):
        if pieceType == 'P':
            return self.SearchPawn(position)
        elif pieceType == 'R':
            return self.SearchRook(position)
        elif pieceType == 'N':
            return self.SearchKnight(position)
        elif pieceType == 'B':
            return self.SearchBishop(position)
        elif pieceType == 'Q':
            return self.SearchQueen(position)
        elif pieceType == 'K':
            return self.SearchKing(position)

    def GetMove(self,move, color):
        """Converts algebraic move into start and end tuples."""
        # Castle?
        if move == 'O-O':
            if color == 'w':
                return ('e',1),('g',1),'castle'
            else:
                return ('e',8),('g',8),'castle'
        elif move == 'O-O-O':
            if color == 'w':
                return ('e',1),('c',1),'castle'
            else:
                return ('e',8),('c',8),'castle'
            

        # Process the string: Figure out what we know
        # [RNBQK]?[a-h][1-8]?[x]?[a-h]?[1-8][=]?[RNBQK]?[+#]

        # Is the string in the proper format
        test = re.search('([RNBQK])?([a-h])?([1-8])?([x])?([a-h])[1-8]([=])?([RNBQK])?(ep)*([+#])*',move)
        if test == None:
            print("Move is in improper format")
            return None

        pieceType = re.findall('^[RNBQK]',move)
        file = re.findall('[a-h]',move)
        rank = re.findall('[1-8]', move)
        capture = re.findall('[x]',move)
        promotion = re.findall('(?<!^)[RNBQK]',move)
        decorator = re.findall('[+#]',move)

        if len(pieceType) == 0:
            pieceType = 'P'
        else:
            pieceType = pieceType[0]

        if len(promotion) > 0:
            special = 'promotion ' + promotion[0]

        if len(file) == 1:
            # we only have a destination file
            startFile = None
            destinationFile = file[0]
        elif len(file) == 2:
            startFile = file[0]
            destinationFile = file[1]
        
        if len(rank) == 1:
            startRank = None
            destinationRank = int(rank[0])
        elif len(file) == 2:
            startRank = int(rank[0])
            destinationRank = int(rank[1])

        # If we have the start position and end postion, then we can quit
        if (startRank != None) and (startFile != None):
            return (startFile,startRank), (destinationFile,destinationRank), special

        # Find the starting position
        allPieces = self._board.GetAllPieces(color)

        # Do we know the start file?
        searchPieces = []
        if startFile != None:
            for current in allPieces:
                if (current[0] == startFile) and str.upper(self._board.GetPiece(current)) == pieceType:
                    searchPieces.append(current)
        else:
            for current in allPieces:
                if str.upper(self._board.GetPiece(current)) == pieceType:
                    searchPieces.append(current)

        for currentPiece in searchPieces:
            possibleMoves = self.SearchPiece(pieceType,currentPiece)

            if (destinationFile,destinationRank) in possibleMoves:
                return currentPiece, (destinationFile,destinationRank), None

        return None
