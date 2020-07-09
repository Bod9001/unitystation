import re
import shutil
print('------------------ Unity dark theme patch 0.9 Windows version ------------- ' )

The_Input  = input(" Want to see cool numbers? Y/N ")
#The_Input = "d"
Show_cool_numbers = False

if The_Input == "y" or The_Input == "Y":
    Show_cool_numbers = True

print('reading unity_x64.pdb')
f = open('unity_x64.pdb', 'rb')
data = f.read()
f.close()

pattern = b"\x47\x65\x74\x53\x6B\x69\x6E\x49\x64\x78\x40" #"GetSkinIdx@"
regex = re.compile(pattern)
Location = bytearray(4)
print('searching unity_x64.pdb')
if Show_cool_numbers:  
    print("searching for", pattern)
for match_obj in regex.finditer(data):
    print('instance found in unity_x64.pdb')
    offset = match_obj.start()
    
    if Show_cool_numbers:
        print("#######################################")
        print ("decimal: {}".format(offset))
        print ("hex(): " + hex(offset))
        print ('formatted hex: {:02X}'.format(offset))
        print("#######################################")
    offset = offset - 7 #Assumption will have to check on different versions
    
    if Show_cool_numbers:
        print("#######################################")
        print ("decimal: {}".format(offset))
        print ("hex(): " + hex(offset))
        print ('formatted hex: {:02X}'.format(offset))
        print("#######################################")

    
    
    for i in range(0,4):
        Location[i] = data[offset+i]
        
    if Show_cool_numbers:  
        print("function offset  ", Location)

f = open('Unity.exe', 'rb')
print('reading Unity.exe')
data = f.read()
f.close()

pattern =  b"\x2E\x74\x65\x78\x74" #".text"
regex = re.compile(pattern)


Text_offset = bytearray(4)

First = False
print('searching Unity.exe')
if Show_cool_numbers:  
    print("searching for", pattern)
    
for match_obj in regex.finditer(data):
    
    if First:
        break
    print('instance found in Unity.exe')
    First = True
    offset = match_obj.start()
    if Show_cool_numbers:
        print("#######################################")
        print ("decimal: {}".format(offset))
        print ("hex(): " + hex(offset))
        print ('formatted hex: {:02X}'.format(offset))
        print("#######################################")

    offset = offset + 20

    for i in range(0,4):
        Text_offset[i] = data[offset+i]
        
    if Show_cool_numbers:  
        print(".text offset " , Text_offset)

Actual_location = int.from_bytes(Text_offset, byteorder='little') +  int.from_bytes(Location, byteorder='little');

if Show_cool_numbers:    
    print("binary byte location in file ", Actual_location )


Inject_code = bytearray(b"\x31\xC0\xFE\xC0\xC3")
if Show_cool_numbers:
    print("Inject_code > ", Inject_code)
    
print("Duplicating Unity.exe... please wait " )
shutil.copyfile('Unity.exe', 'Unity_Patched.exe')
print("Duplicating done " )
print("Patching Unity_Patched.exe " )
f = open('Unity_Patched.exe', "rb+")

f.seek((Actual_location), 0)
f.write(Inject_code)
f.close()
print("done, run Unity_Patched.exe for your nice dark theme" )
    

