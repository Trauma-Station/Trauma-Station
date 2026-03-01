#!/usr/bin/env python3
import argparse, re, os
from collections.abc import Sequence
from pathlib import Path

def file_starts_with(path: str, prefix: bytes) -> bool:
	n = len(prefix)
	# not buffering because it's just reading an exact number
	with open(path, 'rb', buffering=0) as f:
		return f.read(n) == prefix

def slopify(path, prefix: bytes, regex: re.Pattern, header: str) -> int:
	# do nothing if it's already correct
	if file_starts_with(path, prefix):
		return 0

	# read the existing file then replace any incorrect comments with the license reader
	f = Path(path)
	data = f.read_text(encoding='utf-8')
	data = regex.sub(header, data)
	#data = "%s\n%s" % (header, data)
	f.write_text(data, encoding='utf-8')
	print("Updated license headers for " + path)
	return 1

def main(argv: Sequence[str] | None = None) -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument('-c', '--comment', help='Regex for all comments at the start of the file')
	parser.add_argument('-H', '--header', help='Header string to prepend to every file')
	parser.add_argument('filenames', nargs='*', help='Filenames to check')
	args = parser.parse_args(argv)

	comment = re.compile(args.comment, re.S) # S: dotall
	header = args.header + "\n\n" # empty line + joining newline for the actual file contents
	prefix = bytes(header, encoding='utf-8')
	res = 0
	for path in args.filenames:
		res += slopify(path, prefix, comment, header)

	return res

if __name__ == '__main__':
	raise SystemExit(main())
