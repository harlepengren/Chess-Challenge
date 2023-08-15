import re


class Board:
    """Class to manage the chess board."""

    allFiles = ['a','b','c','d','e','f','g','h']

    def __init__(self, inputFen='rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'):
        """Initialize the chess board. Takes one parameter (in FEN) that sets the board. If no parameter is passed,
        the board is initialized to the starting position."""
        self.SetFEN(inputFen)

    def SetFEN(self, inputFen):
        fenElements = re.split('\s',inputFen)
        if len(fenElements) == 0:
            print("Improper format.")
            return None

        self._fen = re.findall("[a-zA-Z1-8]+",fenElements[0])
        # Check whether the board is in the correct format.
        if not self._CheckFenInput():
            print("Improper format")
            return None

        # Create a blank board
        self._board = []*8

        # create the board
        for currentRow in self._ReadRow():
            self._board.append(currentRow)
        
        if len(fenElements) > 1:
            self._currentPlayer = fenElements[1]

        if len(fenElements) > 2:
            self._castle = {'whiteKing': False, 'whiteQueen': False, 'blackKing':False, 'blackQueen':False}
            for character in fenElements[2]:
                if character == 'K':
                    self._castle['whiteKing'] = True
                elif character == 'Q':
                    self._castle['whiteQueen'] = True
                elif character == 'k':
                    self._castle['blackKing'] = True
                elif character == 'q':
                    self._castle['blackQueen'] = True

        # En passant
        if len(fenElements) > 3:
            self._ep = fenElements[3]

        # Half moves
        if len(fenElements) > 4:
            self._halfmoves = int(fenElements[4])

        # Full moves
        if len(fenElements) > 5:
            self._fullmoves = int(fenElements[5])

    def _CheckFenInput(self):
        """Checks whether FEN input is properly formatted. 
        Returns true if properly formatted and false otherwise"""

        if len(self._fen) != 8:
            return False

        for row in self._fen:
            count = 0
            for element in row:
                if str.isalpha(element):
                    count += 1
                elif str.isnumeric(element):
                    count += int(element)
            
            if count != 8:
                return False
        
        return True

    def _ReadRow(self):
        """Generator to read next row in self._fen"""
        currRank = 7

        while currRank >= 0:
            row = []
            position = 0
            while (position < len(self._fen[currRank])):
                nextElemetn = self._fen[position]
                if str.isnumeric(self._fen[currRank][position]):
                    for _ in range(int(self._fen[currRank][position])):
                        row.append('.')
                else:
                    row.append(self._fen[currRank][position])
                position += 1
            yield row

            currRank -= 1
    
    def _ConvertPosition(self,position):
        """Converts tuple input (e.g., (a,1)) into file, rank positions."""
        # Convert file to number
        if not(position[0] in self.allFiles):
            file = None
        else:
            file = self.allFiles.index(position[0])

        if position[1] < 1 or position[1]>8:
            rank = None
        else:
            rank = position[1]-1

        return file, rank
    
    def __str__(self):
        output = str()

        output += '  +'+'-'*17+'+'+'\n'
        for rank in range(8):
            output += str(8-rank) + " + "
            for file in range(8):
                output += self._board[7-rank][file] + ' '            
            output += '+\n'
        output += '  +'+'-'*17+'+\n'
        output += '    a b c d e f g h\n'

        return output

    def PrintBoard(self):
        """Print the current board."""
        print(self)
    
    def GetPiece(self,position):
        """Get the piece at a particular position. The input is a tuple with (file,rank). Example (a,1)"""
        file, rank = self._ConvertPosition(position)

        if file == None or rank == None:
            return None

        piece = self._board[rank][file]

        if piece == '.':
            piece = None

        return piece

    def GetColor(self,position):
        """Get the color of a piece at position."""

        # Get the piece at the position
        piece = self.GetPiece(position)

        if piece == None:
            return None

        if str.upper(piece) == piece:
            return 'w'
        
        return 'b'

    def MovePiece(self,start,end, special=None):
        """Conduct the actual move. Start and end are tuples. Special is used to specify castle, promotion, and en passant."""

        piece = self.GetPiece(start)
        capture = True if self.GetPiece(end) != None else False
        
        startFile, startRank = self._ConvertPosition(start)
        endFile, endRank = self._ConvertPosition(end)

        self._board[startRank][startFile] = '.'
        self._board[endRank][endFile] = piece
        
        if special == 'castle':
            if end[0] == 'g':
                # King side castle move rook to f file
                rookStartFile = 'h'
                rookEndFile = 'f'
            else:
                # Queen side castle, move rook to d file
                rookStartFile = 'a'
                rookEndFile = 'd'

            # Move the rook
            rookPiece = self.GetPiece((rookStartFile,start[1]))
            startRookFile, startRookRank = self._ConvertPosition((rookStartFile,start[1]))
            endRookFile, endRookRank = self._ConvertPosition((rookEndFile,end[1]))
            self._board[startRookRank][startRookFile] = '.'
            self._board[endRookRank][endRookFile] = rookPiece
        elif (special != None) and ('promotion' in special):
            promoPiece = special.split()[1]
            self._board[endRank][endFile] = promoPiece
        elif special == 'ep':
            # Need to remove the pawn that the player captured
            # Removed pawn is at the end file and starting rank
            removeFile, removeRank = self._ConvertPosition((end[0],start[1]))
            self._board[removeRank][removeFile] = '.'
        
        # Update number of move numbers
        # Half moves are number of moves since last capture or pawn advance
        self._halfmoves = 0 if (str.upper(piece) == 'P' or capture) else self._halfmoves + 1
        # Full moves always increment after blackmove
        if str.lower(piece) == piece:
            self._fullmoves += 1
        # Did the king or rook move?
        if piece == 'K':
            self._castle['whiteKing'] = False
            self._castle['whiteQueen'] = False
        elif piece == 'k':
            self._castle['blackKing'] = False
            self._castle['blackQueen'] = False
        
        if piece == 'R':
            # which side
            if start[0] == 'a':
                self._castle['whiteQueen'] = False
            elif start[0] == 'h':
                self._castle['whiteKing'] = False
        elif piece == 'r':
             # which side
            if start[0] == 'a':
                self._castle['blackQueen'] = False
            elif start[0] == 'h':
                self._castle['blackKing'] = False
        
        if str.upper(piece) == 'P':
            # Did the pawn move two
            numSpaces = start[1]-end[1]
            if abs(numSpaces) == 2:
                self._ep = start[0]+'6' if numSpaces == -2 else start[0]+'3'

    def GetAllPieces(self,player):
        """Returns a list of all pieces for a particular player and the corresponding positions on the board."""
        allPieces = []
        pieceTest = str.isupper if player=='w' else str.islower

        for rank in range(8):
            for files in range(8):
                if self._board[rank][files] != '.' and pieceTest(self._board[rank][files]):
                    allPieces.append((self.allFiles[files],rank+1))

        return allPieces

    def ExportFEN(self,currentPlayer):
        """Convert board into FEN string."""
        counter = 0
        fen = ''
        for rank in range(8):
            for file in range(8):
                piece = self._board[rank][file]
                if piece == '.':
                    counter += 1
                else:
                    if counter > 0:
                        fen += str(counter)
                    
                    fen += piece
                    counter = 0
                
            if counter > 0:
                fen += str(counter)
            
            if rank < 7:
                fen += '/'
                counter = 0
        
        fen = fen + " " + currentPlayer + " "

        if self._castle['whiteKing']:
            fen = fen + 'K'
        if self._castle['whiteQueen']:
            fen = fen + 'Q'
        if self._castle['blackKing']:
            fen = fen + 'k'
        if self._castle['blackQueen']:
            fen = fen + 'q'

        fen = fen + " " + self._ep + " " + str(self._halfmoves) + " " + str(self._fullmoves)

        return fen
