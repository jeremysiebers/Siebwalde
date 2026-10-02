from pathlib import Path
import zipfile, copy, collections, re, json

SRC=Path('/mnt/data/JSIF.zip')
OUT=Path('/mnt/data/JSIF_SimpleLoop_4Amp.zip')

with zipfile.ZipFile(SRC,'r') as z:
    original_infos=z.infolist()
    original={i.filename:z.read(i.filename) for i in original_infos if not i.is_dir()}

def rows(file):
    dat=original['JSIF/'+file]
    assert dat.endswith(b'\r\n') and dat.count(b'\n')==dat.count(b'\r\n')
    return dat.decode('cp1252').split('\r\n')[:-1]

def make(file, ls):
    return ('\r\n'.join(ls)+'\r\n').encode('cp1252') if ls else b''

changed={}
# Topology drawing: preserve only four block-associated routes, with one
# continuous upper segment replacing both turnouts and the parallel branch.
baan=[]
sensors_inserted=False
inbv_inserted=False
bzwl_inserted=False
oval_lines={1,2,3,4,8,15,16,17}
new_markers=[
    'INBL\t1\t130\t280\t0\t1\t',
    'INBL\t2\t250\t280\t0\t1\t',
    'INBL\t3\t370\t280\t0\t1\t',
    'INBL\t4\t490\t280\t0\t1\t',
]
new_inbv=[
    'INBV\t1\t4\t\t\t\t1\t',
    'INBV\t2\t1\t\t\t\t2\t',
    'INBV\t3\t2\t\t\t\t3\t',
    'INBV\t4\t3\t\t\t\t4\t',
]
new_bzwl=[f'BZWL\t{100+i}\t0\t0\t{i}\t0\t' for i in range(1,5)]
for line in rows('baan.dba'):
    f=line.split('\t'); typ=f[0]
    if typ=='LIJN':
        n=int(f[1])
        if n not in oval_lines: continue
        if n==2:
            # LIJN 2 is the entire upper block (no turnouts).
            f=['LIJN','2','0','168','28','0',''] if f[2]=='0' else ['LIJN','2','1','498','28','0','']
        baan.append('\t'.join(f))
    elif typ in {'WISS','WSTR'}:
        continue
    elif typ=='PBLK':
        if int(f[1])<=4: baan.append(line)
    elif typ=='LIBL':
        if int(f[1])<=4:
            assert int(f[2]) in oval_lines, line
            baan.append(line)
    elif typ=='INBL':
        if not sensors_inserted:
            baan.extend(new_markers); sensors_inserted=True
    elif typ=='INBV':
        if not inbv_inserted:
            baan.extend(new_inbv); inbv_inserted=True
    elif typ=='BZWL':
        if not bzwl_inserted:
            baan.extend(new_bzwl); bzwl_inserted=True
    else: baan.append(line)
assert sensors_inserted and inbv_inserted and bzwl_inserted
changed['JSIF/baan.dba']=make('baan.dba',baan)

# Koploper's block and predecessor/next-block relationships.
feedback={1:101,2:102,3:103,4:104}
prev={1:4,2:1,3:2,4:3}
allowed_blri={(2,4,1),(3,1,2),(4,2,3),(1,3,4)}
blok=[]
for line in rows('blok.dba'):
    f=line.split('\t'); typ=f[0]
    if typ=='BLKT': blok.append(line)
    elif typ=='BLOK':
        block=int(f[4])
        if block not in feedback: continue
        f[19]=str(feedback[block])
        blok.append('\t'.join(f))
    elif typ=='BLVN':
        block=int(f[2]); p=int(f[3])
        if block not in feedback or prev[block]!=p: continue
        assert '@' in f[9]
        prefix=f[9].split('@',1)[0]
        f[9]=prefix+'@'+str(feedback[block])+'#0#0#0#0'
        blok.append('\t'.join(f))
    elif typ=='BLRI':
        if tuple(int(f[i]) for i in (1,2,3)) in allowed_blri:
            blok.append(line)
    elif typ=='DLCK':
        # Prior deadlock settings concerned the now-removed passing loop.
        continue
    else:
        raise AssertionError('Unrecognized block record: '+typ)
changed['JSIF/blok.dba']=make('blok.dba',blok)

# Preserve all generic Koploper definitions and locomotive settings; remove
# only block 5 and the block-1 predecessor reference to that passing branch.
kopd=[]
for line in rows('kopd.dba'):
    f=line.split('\t'); typ=f[0]
    if typ=='BLAV5': continue
    if typ=='BLAV1':
        assert f[27]=='4,5',f[27]
        f[27]='4';line='\t'.join(f)
    kopd.append(line)
changed['JSIF/kopd.dba']=make('kopd.dba',kopd)

# Place the two preserved locomotives in available blocks, not in deleted block 5.
locr=[]
for line in rows('locr.dba'):
    f=line.split('\t')
    assert f[0] in ('<1>','<2>')
    f[2]='1' if f[0]=='<1>' else '3'
    locr.append('\t'.join(f))
changed['JSIF/locr.dba']=make('locr.dba',locr)

ini=[]; section=None
for line in rows('kopl.ini'):
    if line.startswith('[') and line.endswith(']'):section=line
    if section=='[UniekNr]' and line.startswith('blok='):
        assert line=='blok=5'
        line='blok=4'
    ini.append(line)
changed['JSIF/kopl.ini']=make('kopl.ini',ini)

# Historical speed calibration is specific to the former 5-block, 2-sensor
# layout. Empty valid text table ensures no stale calibration is reused.
changed['JSIF/snel.dba']=b''
# save_oud.txt is an *old* runtime snapshot containing removed block 5 and
# old turnout references. Do not ship it in the replacement data directory.
omit={'JSIF/save_oud.txt'}

# Preserve directory root, original file names, zip compression methods, and
# all untouched bytes. zipfile recomputes sizes/CRC from actual new payloads.
with zipfile.ZipFile(OUT,'w',allowZip64=True) as z:
    for oi in original_infos:
        if oi.filename in omit:continue
        info=copy.copy(oi)
        info.CRC=0;info.file_size=0;info.compress_size=0
        payload=changed.get(oi.filename,original.get(oi.filename,b''))
        z.writestr(info,payload)

# Independent archive round-trip and source preservation assertions.
with zipfile.ZipFile(OUT) as z:
    assert z.testzip() is None
    assert z.namelist()==[x.filename for x in original_infos if x.filename not in omit]
    for file, data in changed.items():
        assert z.read(file)==data, file
    for filename, data in original.items():
        if filename not in changed and filename not in omit:
            assert z.read(filename)==data,filename
print('CREATED',OUT,OUT.stat().st_size)
print('CHANGED_FILES',','.join(changed))
print('OMITTED',','.join(omit))
print('ZIP_CRC_STATUS','PASS')
