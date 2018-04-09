import msvcrt

def readChar(echo=True):
    "Get a single character on Windows."
    while msvcrt.kbhit():
        msvcrt.getch()
    ch = msvcrt.getch()
    while ch in b'\x00\xe0':
        msvcrt.getch()
        ch = msvcrt.getch()
    if echo:
        msvcrt.putch(ch)
    return ch.decode()

while 1:
	c = readChar(False)
	if (c == "q"):
		break
	print(c)
