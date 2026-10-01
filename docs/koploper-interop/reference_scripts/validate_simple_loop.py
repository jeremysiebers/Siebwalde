from pathlib import Path
from collections import Counter, defaultdict
import zipfile, re, hashlib
src=Path('/mnt/data/JSIF.zip')
out=Path('/mnt/data/JSIF_SimpleLoop_4Amp.zip')

with zipfile.ZipFile(out) as z:
 assert z.testzip() is None
 def rr(filename):
  raw=z.read('JSIF/'+filename)
  if not raw:return []
  assert raw.endswith(b'\r\n') and raw.count(b'\r\n')==raw.count(b'\n'), filename
  return [r.split('\t') for r in raw.decode('cp1252').split('\r\n')[:-1]]
 baan=rr('baan.dba');blok=rr('blok.dba');kopd=rr('kopd.dba');ini=rr('kopl.ini');locr=rr('locr.dba')
 by=lambda rows,typ:[f for f in rows if f[0]==typ]
 blocks=by(blok,'BLOK')
 assert [int(f[4]) for f in blocks]==[1,2,3,4]
 assert [f[19] for f in blocks]==['101','102','103','104']
 assert not by(blok,'DLCK')
 blvn=by(blok,'BLVN')
 assert len(blvn)==4
 pred={int(f[2]):int(f[3]) for f in blvn}
 assert pred=={1:4,2:1,3:2,4:3},pred
 for f in blvn:
  b=int(f[2]);s=f[9]
  assert s.startswith('AA\xff\xff@') and s=='AA\xff\xff@'+str(100+b)+'#0#0#0#0',(b,s)
 blri=by(blok,'BLRI')
 assert len(blri)==4
 assert {tuple(map(int,(f[1],f[2],f[3]))) for f in blri}=={(2,4,1),(3,1,2),(4,2,3),(1,3,4)}
 print('01 Block topology: 4-block closed ring PASS')
 # Topology track graph (each LIJN id supplies a pair of endpoints).
 points=defaultdict(list)
 for f in by(baan,'LIJN'):
  lineid=int(f[1]);p=(int(f[3]),int(f[4]));points[lineid].append((int(f[2]),p))
 assert len(points)==8 and all(len(x)==2 for x in points.values())
 edges={lineid:tuple(p for _end,p in sorted(coords)) for lineid,coords in points.items()}
 adj=defaultdict(set)
 for p,q in edges.values():
  assert p!=q
  adj[p].add(q);adj[q].add(p)
 assert len(adj)==8 and all(len(v)==2 for v in adj.values())
 reached=set();todo=[next(iter(adj))]
 while todo:
  p=todo.pop()
  if p in reached:continue
  reached.add(p);todo.extend(adj[p]-reached)
 assert len(reached)==len(adj)
 assert len(by(baan,'PBLK'))==4 and not by(baan,'WISS') and not by(baan,'WSTR')
 assert {int(f[1]) for f in by(baan,'PBLK')}=={1,2,3,4}
 for f in by(baan,'LIBL'):
  assert int(f[1]) in {1,2,3,4} and int(f[2]) in edges
 print('02 Drawing: single connected oval; no turnouts/branch/signals PASS')
 # One ECoS sensor per block/route, linked to a single indicator.
 sensors=by(baan,'BZWL');markers=by(baan,'INBL');paths=by(baan,'INBV')
 assert [int(f[1]) for f in sensors]==[101,102,103,104]
 assert [int(f[4]) for f in sensors]==[1,2,3,4]
 assert [int(f[1]) for f in markers]==[1,2,3,4]
 assert len(paths)==4
 for f in paths:
  b=int(f[1]);p=int(f[2]);marker=f[6]
  assert pred[b]==p and marker==str(b),(b,p,marker)
 print('03 Feedback: 1 sensor per block, ECoS 1.01-1.04, all indicator/route refs PASS')
 blav=[f for f in kopd if re.fullmatch(r'BLAV\d+',f[0])]
 assert [f[0] for f in blav]==['BLAV1','BLAV2','BLAV3','BLAV4']
 assert {int(f[0][4:]):int(f[27]) for f in blav}==pred
 assert {int(f[2]) for f in locr}=={1,3}
 assert [f[0] for f in locr]==['<1>','<2>']
 loco=[f for f in kopd if re.fullmatch('LOKO[12]',f[0])]
 assert [(int(f[5]),int(f[40])) for f in loco]==[(1,1000),(2,1001)]
 assert any(f==['blok=4'] for f in ini)
 print('04 Cross-tables: BLAV/locos/unique-block-counter PASS')
 assert len(z.read('JSIF/snel.dba'))==0
 assert 'JSIF/save_oud.txt' not in z.namelist()
 print('05 Former speed-calibration & stale state: removed/reset PASS')
 with zipfile.ZipFile(src) as old:
  for name in ['JSIF/koplg.dba']:
   assert z.read(name)==old.read(name)
 print('06 ZIP integrity (CRC), original directory root & untouched data PASS')
 print('OUTPUT_BYTES',out.stat().st_size)
 print('SHA256',hashlib.sha256(out.read_bytes()).hexdigest())
