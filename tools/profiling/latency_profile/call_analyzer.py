#!/usr/bin/env python2.7
# Copyright 2015, Google Inc.
# All rights reserved.
#
# Redistribution and use in source and binary forms, with or without
# modification, are permitted provided that the following conditions are
# met:
#
#     * Redistributions of source code must retain the above copyright
# notice, this list of conditions and the following disclaimer.
#     * Redistributions in binary form must reproduce the above
# copyright notice, this list of conditions and the following disclaimer
# in the documentation and/or other materials provided with the
# distribution.
#     * Neither the name of Google Inc. nor the names of its
# contributors may be used to endorse or promote products derived from
# this software without specific prior written permission.
#
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
# "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
# LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
# A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
# OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
# SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
# LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
# DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
# THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
# (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
# OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

import argparse
import collections
import hashlib
import itertools
import json
import math
import tabulate
import time


SELF_TIME = object()
TIME_FROM_SCOPE_START = object()
TIME_TO_SCOPE_END = object()
TIME_FROM_STACK_START = object()
TIME_TO_STACK_END = object()
TIME_FROM_LAST_IMPORTANT = object()


argp = argparse.ArgumentParser(description='Process output of basic_prof builds')
argp.add_argument('--source', default='latency_trace.txt', type=str)
argp.add_argument('--fmt', choices=tabulate.tabulate_formats, default='simple')
args = argp.parse_args()

class LineItem(object):

  def __init__(self, line, indent):
    self.tag = line['tag']
    self.indent = indent
    self.start_time = line['t']
    self.end_time = None
    self.important = line['imp']
    self.filename = line['file']
    self.fileline = line['line']
    self.callPtr = line.get('callPtr', 0)
    self.cqTagPtr = line.get('cqTagPtr', 0)
    self.times = {}

lines = 0


def _split_call_entries(entries):
  """Splits entries by call in case callPtr gets reused"""
  result = []
  for entry in entries:
    if entry['tag'] == 'server_request_call' and entry['type'] == '{':
      result.append([entry])
    else:
      result[-1].append(entry)
  return result

all_entries = []
with open(args.source) as f:
  for line in f:
    lines += 1
    entry = json.loads(line)
    all_entries.append(entry)
 
entries_by_call = {}
all_entries.sort(key=lambda entry: entry['callPtr'])
for callPtr, group in itertools.groupby(all_entries, lambda entry: entry['callPtr']):
  calls = _split_call_entries(group)
  if len(calls) == 1:
    entries_by_call[callPtr] = calls[0]
  else:
    for idx, call in enumerate(calls):
      entries_by_call['%s_%s' % (callPtr, idx)] = call   

  
i = 0
for k,v in entries_by_call.iteritems():  
  
  if i > 100:
    break

  print k
  for entry in v:
    print(entry)
  print()
  i+=1
  
    





#if args.fmt == 'html':
#  print '<html>'
#  print '<head>'
#  print '<title>Profile Report</title>'
#  print '</head>'

#accounted_for = 0
#for cs in call_stacks:
#  if args.fmt in BANNER:
#    print BANNER[args.fmt] % {
#        'count': cs.count,
#    }
#  header, _ = zip(*FORMAT)
#  table = []
#  for line in cs.lines:
#    fields = []
#    for _, fn in FORMAT:
#      fields.append(fn(line))
#    table.append(fields)
#  print tabulate.tabulate(table, header, tablefmt=args.fmt)
#  accounted_for += cs.count
#  if accounted_for > .99 * total_stacks:
#    break

#if args.fmt == 'html':
#  print '</html>'

