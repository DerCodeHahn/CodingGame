import glob
files = glob.glob('**/*.cs', recursive=True)
output = []
using = []

for file_name in files:
    if file_name.startswith("obj"):
        continue
    if file_name.startswith("_"):
        continue

    output.append("/*")
    output.append(file_name)
    output.append("*/")
    output.append("")
        
    file = open(file_name, 'r', encoding="utf8")
    lines = file.readlines()
    for line in lines:
        if line.startswith("using"):
            using.append(line.rstrip())
        else:
            output.append(line.rstrip())

    output.append("")
    output.append("")

with open('output.txt', 'w', encoding="utf8") as outfile:
    outfile.write("\n".join(set(using)))
    outfile.write("\n")
    outfile.write("\n".join(output))