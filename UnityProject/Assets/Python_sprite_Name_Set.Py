import os
import time

start_time = time.time()
path = 'Textures'
for root, dirs, files in os.walk(path):
    print(root)
    for _name in files:
        if _name.endswith((".png.meta")):
           
            newFolder = os.path.join(root, _name)
            print(newFolder)
            f = open(newFolder, 'r+')
            contents = f.read()
            f.close()
            #print(contents)
            contents = contents.replace('second: ','second: '+os.path.basename(root)+ "_" )
            contents = contents.replace('name: ','name: '+os.path.basename(root)+ "_" )
            metaFile = open(newFolder,"w+")
            metaFile.write(contents)
            metaFile.close()

print("--- %s seconds ---" % (time.time() - start_time)) #Total time
