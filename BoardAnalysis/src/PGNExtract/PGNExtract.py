def ProcessFile(filename, writeDirectory):
    f = open(filename,"r")
    fileCounter = 0

    currentLine = f.readline()
    while(currentLine != "" and fileCounter < 1000):
        if '[Event' in currentLine:
            # Create a new file
            output = open(writeDirectory+'/'+'test'+str(fileCounter)+'.pgn',"w")
            fileCounter += 1

        output.write(currentLine)
        currentLine = f.readline()

    f.close()