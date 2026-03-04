import pathlib
p = pathlib.Path(r"C:/Users/Ernesto/Desktop/Колясик/diagrams/gen_classdiagram.py")
c = p.read_text(encoding="utf-8")
c = c.replace("->|>", "-|>")
c = c.replace("-->", "-|>")
p.write_text(c, encoding="utf-8")
print("Done")
