#!/usr/bin/env python3
"""Parse actual C# and Java with tree-sitter. Syntax only, not type/API compilation."""
from pathlib import Path
from tree_sitter import Language, Parser
import tree_sitter_c_sharp
import tree_sitter_java

root=Path(__file__).resolve().parent.parent
parsers={'.cs':Parser(Language(tree_sitter_c_sharp.language())),'.java':Parser(Language(tree_sitter_java.language()))}
failures=[];count=0
for path in (root/'Overlay').rglob('*'):
 if path.suffix not in parsers:continue
 count+=1;tree=parsers[path.suffix].parse(path.read_bytes())
 def scan(node):
  if node.type=='ERROR' or node.is_missing:failures.append(f'{path.relative_to(root)}:{node.start_point.row+1}: syntax error {node.type}')
  for child in node.children:scan(child)
 scan(tree.root_node)
if failures:raise SystemExit('\n'.join(failures))
print(f'PASS: {count} C#/Java files parsed (not Unity or Android compilation)')
