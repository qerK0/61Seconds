import numpy as np, os, sys
from scipy.signal import butter, sosfilt
from scipy.io import wavfile

SR = 44100
rng = np.random.default_rng(61)
OUT = sys.argv[1]
# Optional name prefix: only files starting with it are written (e.g. "sfx_menu_" adds the menu sounds
# without touching the others). The rng still advances the same way, so every file stays deterministic.
ONLY = sys.argv[2] if len(sys.argv) > 2 else None
def wanted(name): return ONLY is None or name.startswith(ONLY)
os.makedirs(OUT, exist_ok=True)

def t(d): return np.arange(int(SR * d)) / SR
def env(n, a=0.002, r=0.1, shape=1.0):
    x = np.arange(n) / SR
    e = np.minimum(1, x / max(a, 1e-4)) * np.exp(-x / r)
    return e ** shape
def bp(x, lo, hi, o=2): return sosfilt(butter(o, [lo, hi], 'band', fs=SR, output='sos'), x)
def lp(x, f, o=2): return sosfilt(butter(o, f, 'low', fs=SR, output='sos'), x)
def hp(x, f, o=2): return sosfilt(butter(o, f, 'high', fs=SR, output='sos'), x)
def noise(d): return rng.uniform(-1, 1, int(SR * d))
def sweep(f0, f1, d, curve='exp'):
    tt = t(d)
    f = f0 * (f1 / f0) ** (tt / d) if curve == 'exp' else f0 + (f1 - f0) * tt / d
    return np.sin(2 * np.pi * np.cumsum(f) / SR)
def saw(f, d):
    tt = t(d); return 2 * ((tt * f) % 1) - 1
def fade(x, fi=0.003, fo=0.02):
    n = len(x); a = int(SR * fi); b = int(SR * fo)
    x = x.copy()
    if a: x[:a] *= np.linspace(0, 1, a)
    if b: x[-b:] *= np.linspace(1, 0, b)
    return x
def place(buf, x, at):
    i = int(SR * at); buf[i:i + len(x)] += x[:len(buf) - i]
def save(name, x, peak=0.89):
    if not wanted(name): return
    x = fade(x)
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    wavfile.write(os.path.join(OUT, name + '.wav'), SR, (x * 32767).astype(np.int16))
    print(name, round(len(x) / SR, 2), 's')

# UI click: short wooden tick
d = 0.06
x = bp(noise(d), 1800, 5000) * env(int(SR*d), 0.0005, 0.006)
x += 0.6 * np.sin(2*np.pi*1400*t(d)) * env(int(SR*d), 0.0005, 0.012)
save('sfx_choice_click', x)

# Denied: two dull descending beeps
buf = np.zeros(int(SR*0.32))
for i, f in enumerate([330, 247]):
    d = 0.12; tt = t(d)
    s = np.sign(np.sin(2*np.pi*f*tt)) * 0.5 + np.sin(2*np.pi*f*tt)
    place(buf, lp(s, 2000) * env(len(tt), 0.003, 0.08), i * 0.15)
save('sfx_denied', buf)

# Countdown tick: clock tick (crisp) — loop-friendly single tick
d = 0.08
x = bp(noise(d), 2500, 7000, 4) * env(int(SR*d), 0.0003, 0.004)
x += 0.5 * np.sin(2*np.pi*1150*t(d)) * env(int(SR*d), 0.0003, 0.015)
save('sfx_countdown_tick', x)

# QTE hit: bright blip
d = 0.12; tt = t(d)
x = (np.sin(2*np.pi*988*tt) + 0.3*np.sin(2*np.pi*1976*tt)) * env(len(tt), 0.002, 0.04)
save('sfx_qte_hit', x, 0.8)

# QTE miss: buzzy downward bonk
d = 0.28
x = lp(saw(1, d) * 0, 1000)
f = np.linspace(180, 90, int(SR*d))
ph = np.cumsum(f) / SR
x = lp(2*((ph) % 1) - 1, 1200) * env(int(SR*d), 0.003, 0.12)
x += 0.4 * bp(noise(d), 100, 600) * env(int(SR*d), 0.001, 0.05)
save('sfx_qte_miss', x)

# QTE success: rising arpeggio C5 E5 G5 C6
buf = np.zeros(int(SR*0.7))
for i, f in enumerate([523.25, 659.25, 783.99, 1046.5]):
    d = 0.35 if i == 3 else 0.18; tt = t(d)
    s = (np.sin(2*np.pi*f*tt) + 0.25*np.sin(2*np.pi*2*f*tt) + 0.1*np.sin(2*np.pi*3*f*tt))
    place(buf, s * env(len(tt), 0.003, 0.1 if i < 3 else 0.2), i * 0.07)
save('sfx_qte_success', buf, 0.8)

# Building alarm: two-tone siren (loopable, 1.2s = 2 cycles)
d = 1.2; tt = t(d)
f = np.where((tt % 0.6) < 0.3, 880.0, 660.0)
ph = np.cumsum(f) / SR
s = np.sin(2*np.pi*ph) + 0.35*np.sign(np.sin(2*np.pi*ph))
gate = np.ones_like(tt)
for k in range(4):  # tiny dips between tones
    i = int(SR*(k*0.3)); gate[i:i+int(SR*0.01)] = np.linspace(0.3, 1, int(SR*0.01))
x = lp(s * gate, 3500)
if wanted('sfx_building_alarm_loop'): wavfile.write(os.path.join(OUT, 'sfx_building_alarm_loop.wav'), SR, (x/np.max(np.abs(x))*0.7*32767).astype(np.int16)); print('sfx_building_alarm_loop 1.2 s')

# Death sting: low dissonant orchestral hit + timpani boom + sad slide-whistle drop (slightly absurd)
D = 2.4; buf = np.zeros(int(SR*D))
d = 1.8; n = int(SR*d)
chord = sum(saw(f, d) for f in [55, 110, 116.54, 164.81, 220])
place(buf, lp(chord, 900, 4) * env(n, 0.005, 0.55), 0)
boom = sweep(90, 38, 1.2) * env(int(SR*1.2), 0.002, 0.35)
place(buf, 1.6 * boom, 0)
place(buf, 0.6 * lp(noise(0.3), 400) * env(int(SR*0.3), 0.001, 0.06), 0)
slide = sweep(900, 260, 0.7) * np.hanning(int(SR*0.7)) * (1 + 0.15*np.sin(2*np.pi*6*t(0.7)))
place(buf, 0.35 * slide, 1.0)
save('sfx_death_sting', buf)

# Rewind: VHS/tape rewind — whirring noise + fast rising chirps + warble
D = 1.4; tt = t(D)
whir = bp(noise(D), 600, 4000) * (0.5 + 0.5*np.sin(2*np.pi*23*tt))
chirp_f = 400 * (6 ** ((tt*7) % 1))  # repeating rising chirps
chirps = np.sin(2*np.pi*np.cumsum(chirp_f)/SR) * 0.45
motor = np.sin(2*np.pi*np.cumsum(120 + 500*tt/D)/SR) * 0.3
x = (0.6*whir + chirps + motor) * np.minimum(1, tt/0.08) * np.minimum(1, (D-tt)/0.25)
save('sfx_rewind', x)

# Glass shatter: crash transient + many glass tinkles
D = 1.5; buf = np.zeros(int(SR*D))
place(buf, hp(noise(0.25), 1500) * env(int(SR*0.25), 0.0005, 0.05), 0)
place(buf, 0.6 * bp(noise(0.08), 300, 2000) * env(int(SR*0.08), 0.0005, 0.02), 0)
for _ in range(70):
    at = rng.exponential(0.18);
    if at > D - 0.2: continue
    f = rng.uniform(2500, 9000); d = rng.uniform(0.04, 0.2); tt2 = t(d)
    s = np.sin(2*np.pi*f*tt2) * env(len(tt2), 0.0003, d/4) * rng.uniform(0.1, 0.5)
    s += 0.3 * hp(noise(d), 4000) * env(len(tt2), 0.0003, 0.01)
    place(buf, s, at)
save('sfx_glass_shatter', buf)

# Descend: rope sliding through hands / friction zip
D = 1.6; tt = t(D)
am = 0.6 + 0.4*np.abs(np.sin(2*np.pi*3.2*tt)) + 0.15*rng.standard_normal(len(tt)).cumsum()/300
x = bp(noise(D), 800, 3500) * am
x += 0.3 * bp(noise(D), 150, 500) * am
x *= np.minimum(1, tt/0.1) * np.minimum(1, (D-tt)/0.3)
x = x * np.linspace(0.7, 1.0, len(tt))
place(x, 0.8 * lp(noise(0.2), 300) * env(int(SR*0.2), 0.001, 0.06), D - 0.25)  # land thump
save('sfx_descend', x)

# Fire / gas explosion: boom + roar + crackle tail
D = 3.0; buf = np.zeros(int(SR*D))
place(buf, 2.0 * sweep(70, 28, 1.5) * env(int(SR*1.5), 0.001, 0.5), 0)
roar = lp(noise(2.6), 900, 4) * env(int(SR*2.6), 0.005, 0.7)
place(buf, 1.4 * roar, 0)
place(buf, 0.8 * hp(noise(0.12), 2000) * env(int(SR*0.12), 0.0005, 0.03), 0)
for _ in range(120):
    at = rng.uniform(0.2, D - 0.1); d = 0.01
    place(buf, rng.uniform(0.05, 0.35) * bp(noise(d), 2000, 7000) * env(int(SR*d), 0.0002, 0.002) * (1 - at/D), at)
save('sfx_fire_explosion', buf)

# Search: rummaging through furniture — cloth rustle bursts + soft wood knocks/creaks (~1.2s, loop-safe ends)
D = 1.2; buf = np.zeros(int(SR*D))
for _ in range(14):
    at = rng.uniform(0.0, D - 0.18); d = rng.uniform(0.06, 0.18); n = int(SR*d)
    rustle = bp(noise(d), 900, 5200) * np.hanning(n) * rng.uniform(0.25, 0.6)
    place(buf, rustle, at)
for _ in range(5):
    at = rng.uniform(0.05, D - 0.12); d = 0.09; tt2 = t(d)
    knock = (np.sin(2*np.pi*rng.uniform(170, 260)*tt2) + 0.4*bp(noise(d), 300, 1500)) * env(len(tt2), 0.001, 0.025)
    place(buf, 0.55 * knock, at)
creak_f = 420 + 60*np.sin(2*np.pi*1.3*t(0.35))
creak = lp(np.sign(np.sin(2*np.pi*np.cumsum(creak_f)/SR)) * 0.2, 1800) * np.hanning(int(SR*0.35))
place(buf, creak, 0.55)
buf *= np.minimum(1, t(D)/0.03) * np.minimum(1, (D - t(D))/0.08)
save('sfx_search', buf, 0.7)

# Car alarm (loopable, 2.0s): classic two-tone whoop + horn honks, slightly distant (low-passed)
D = 2.0; tt = t(D)
f = np.where((tt % 0.5) < 0.25, 1050.0, 780.0) * (1 + 0.04*np.sin(2*np.pi*14*tt))
ph = np.cumsum(f) / SR
s = 0.6*np.sign(np.sin(2*np.pi*ph)) + 0.5*np.sin(2*np.pi*ph)
x = lp(s, 2600) * 0.8
x = x / (np.max(np.abs(x)) + 1e-9)
if wanted('sfx_car_alarm_loop'): wavfile.write(os.path.join(OUT, 'sfx_car_alarm_loop.wav'), SR, (x*0.6*32767).astype(np.int16)); print('sfx_car_alarm_loop 2.0 s')

# ---------- main menu / end screen (task 15) ----------
# Menu hover: a very soft, high tick
d = 0.045
x = bp(noise(d), 3500, 9000) * env(int(SR*d), 0.0003, 0.004) + 0.3 * np.sin(2*np.pi*2400*t(d)) * env(int(SR*d), 0.0003, 0.006)
save('sfx_menu_hover', x, 0.45)

# Menu press: a heavy click with a low thump under it
d = 0.3
x = 0.7 * bp(noise(d), 1500, 5000) * env(int(SR*d), 0.0004, 0.008)
x += np.sin(2*np.pi*np.cumsum(np.linspace(120, 45, int(SR*d)))/SR) * env(int(SR*d), 0.002, 0.09)
save('sfx_menu_press', x, 0.8)

# Seismograph spike: a soft, distant thud
d = 0.6
x = lp(noise(d), 180, 4) * env(int(SR*d), 0.004, 0.14) * 3 + 0.6 * np.sin(2*np.pi*np.cumsum(np.linspace(70, 38, int(SR*d)))/SR) * env(int(SR*d), 0.003, 0.18)
save('sfx_menu_thud', x, 0.6)

# End screen punchline: a deep boom with a long rumbling tail
D = 3.0
x = np.sin(2*np.pi*np.cumsum(np.linspace(62, 28, int(SR*D)))/SR) * env(int(SR*D), 0.006, 0.7)
x += 1.5 * lp(noise(D), 140, 4) * env(int(SR*D), 0.01, 1.1)
x += 0.25 * bp(noise(D), 300, 1200) * env(int(SR*D), 0.002, 0.06)
save('sfx_menu_boom', x, 0.9)

# Typewriter: a soft key tick
d = 0.05
x = bp(noise(d), 1200, 4500) * env(int(SR*d), 0.0003, 0.006) + 0.25 * np.sin(2*np.pi*900*t(d)) * env(int(SR*d), 0.0003, 0.01)
save('sfx_menu_type', x, 0.4)

# Ambient rumble loop (8 s): low brown-ish noise with slow swells; the ends crossfade so it loops cleanly
D = 8.0; n = int(SR*D)
raw = lp(np.cumsum(noise(D + 1.0)) * 0.02, 120, 4)
raw = raw - lp(raw, 18, 2)
swell = 0.7 + 0.3 * np.sin(2*np.pi*t(D + 1.0)/4.0)
raw = raw * swell
xf = int(SR*1.0)
loop = raw[:n].copy()
loop[:xf] = loop[:xf] * np.linspace(0, 1, xf) + raw[n:n+xf] * np.linspace(1, 0, xf)
if wanted('sfx_menu_rumble_loop'):
    loop = loop / (np.max(np.abs(loop)) + 1e-9) * 0.7
    wavfile.write(os.path.join(OUT, 'sfx_menu_rumble_loop.wav'), SR, (loop * 32767).astype(np.int16)); print('sfx_menu_rumble_loop 8.0 s')

# Gas leak hiss loop (4 s): bright filtered noise with a slow flutter; the ends crossfade so it loops cleanly
D = 4.0; n = int(SR*D)
raw = bp(noise(D + 0.5), 2500, 9000, 3) * (0.85 + 0.15 * np.sin(2*np.pi*t(D + 0.5)*0.7))
xf = int(SR*0.5)
loop = raw[:n].copy()
loop[:xf] = loop[:xf] * np.linspace(0, 1, xf) + raw[n:n+xf] * np.linspace(1, 0, xf)
if wanted('sfx_gas_hiss_loop'):
    loop = loop / (np.max(np.abs(loop)) + 1e-9) * 0.5
    wavfile.write(os.path.join(OUT, 'sfx_gas_hiss_loop.wav'), SR, (loop * 32767).astype(np.int16)); print('sfx_gas_hiss_loop 4.0 s')

# Valve closing: a short metal creak (rising squeal) and a dull clunk at the stop
d = 0.55
f = 520 + 340 * (t(d) / d) ** 1.5 + 25 * np.sin(2*np.pi*11*t(d))
x = bp(np.sign(np.sin(2*np.pi*np.cumsum(f)/SR)) * 0.3, 400, 3200) * np.hanning(int(SR*d))
buf = np.zeros(int(SR*0.8))
place(buf, x, 0.0)
place(buf, lp(noise(0.12), 600, 3) * env(int(SR*0.12), 0.001, 0.03) * 2.5 + 0.6 * np.sin(2*np.pi*140*t(0.12)) * env(int(SR*0.12), 0.001, 0.05), 0.5)
save('sfx_valve_creak', buf, 0.8)

# Notebook page: a short dry paper rustle (two crinkly bursts)
d = 0.42
buf = np.zeros(int(SR*d))
for at, ln in ((0.0, 0.16), (0.14, 0.24)):
    n = int(SR*ln)
    crinkle = bp(noise(ln), 1800, 7000, 2) * (rng.uniform(0, 1, n) > 0.55) * np.hanning(n)
    place(buf, crinkle + 0.4 * bp(noise(ln), 400, 1500) * np.hanning(n), at)
save('sfx_paper_rustle', buf, 0.6)

# Street wind loop (8 s): low howling band-passed noise with slow gusts; the ends crossfade
D = 8.0; n = int(SR*D)
raw = bp(noise(D + 1.0), 180, 900, 2) * (0.55 + 0.45 * np.sin(2*np.pi*t(D + 1.0)/5.3) ** 2)
raw += 0.4 * bp(noise(D + 1.0), 900, 2400, 2) * (0.5 + 0.5 * np.sin(2*np.pi*t(D + 1.0)/3.1 + 1.0))
xf = int(SR*1.0)
loop = raw[:n].copy()
loop[:xf] = loop[:xf] * np.linspace(0, 1, xf) + raw[n:n+xf] * np.linspace(1, 0, xf)
if wanted('sfx_wind_loop'):
    loop = loop / (np.max(np.abs(loop)) + 1e-9) * 0.55
    wavfile.write(os.path.join(OUT, 'sfx_wind_loop.wav'), SR, (loop * 32767).astype(np.int16)); print('sfx_wind_loop 8.0 s')

# Ambulance / emergency siren loop (6 s): a two-tone wail, low-passed so it can sound distant
D = 6.0; n = int(SR*D)
f = 700 + 260 * (np.sin(2*np.pi*t(D)/1.5) > 0)
x = np.sin(2*np.pi*np.cumsum(f)/SR) + 0.35 * np.sin(2*np.pi*2*np.cumsum(f)/SR)
x = lp(x, 2600, 2)
if wanted('sfx_siren_loop'):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.6
    wavfile.write(os.path.join(OUT, 'sfx_siren_loop.wav'), SR, (x * 32767).astype(np.int16)); print('sfx_siren_loop 6.0 s')

# Distant metal groan / drip: a slow bending creak with a ring
d = 1.6
f = 160 + 60 * np.sin(2*np.pi*0.6*t(d)) + 40 * (t(d) / d)
x = bp(np.sign(np.sin(2*np.pi*np.cumsum(f)/SR)) * 0.4, 200, 1800) * np.hanning(int(SR*d))
x += 0.3 * np.sin(2*np.pi*1180*t(d)) * env(int(SR*d), 0.002, 0.5)
save('sfx_metal_groan', x, 0.6)
