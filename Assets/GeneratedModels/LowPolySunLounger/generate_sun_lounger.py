import math
from pathlib import Path

out_dir = Path(r"C:\KSW\GameLab_Week3_Personal\Assets\GeneratedModels\LowPolySunLounger")
out_dir.mkdir(parents=True, exist_ok=True)
obj_path = out_dir / "SunLounger_LowPoly.obj"
mtl_path = out_dir / "SunLounger_LowPoly.mtl"

verts = []
faces = []
materials = []
current_mat = None

def rot_z(v, deg):
    x, y, z = v
    r = math.radians(deg)
    c = math.cos(r)
    s = math.sin(r)
    return (x*c - y*s, x*s + y*c, z)

def add_box(name, center, size, mat="beige", rot_deg=0):
    global current_mat
    cx, cy, cz = center
    sx, sy, sz = size
    hx, hy, hz = sx/2, sy/2, sz/2
    local = [
        (-hx,-hy,-hz),( hx,-hy,-hz),( hx,-hy, hz),(-hx,-hy, hz),
        (-hx, hy,-hz),( hx, hy,-hz),( hx, hy, hz),(-hx, hy, hz)
    ]
    start = len(verts) + 1
    for p in local:
        x,y,z = rot_z(p, rot_deg)
        verts.append((cx+x, cy+y, cz+z))
    # quads; OBJ accepts quads and Unity triangulates. Keeps file simple/low-poly.
    quads = [
        (1,2,3,4), # bottom
        (5,8,7,6), # top
        (1,5,6,2),
        (2,6,7,3),
        (3,7,8,4),
        (4,8,5,1),
    ]
    for q in quads:
        faces.append((mat, tuple(start + i - 1 for i in q), name))

def add_wedge_leg(name, center, top_size, bottom_size, height, mat="beige"):
    cx, cy, cz = center
    tx, tz = top_size
    bx, bz = bottom_size
    y_top = cy + height/2
    y_bottom = cy - height/2
    start = len(verts) + 1
    points = [
        (-bx/2,y_bottom,-bz/2),( bx/2,y_bottom,-bz/2),( bx/2,y_bottom, bz/2),(-bx/2,y_bottom, bz/2),
        (-tx/2,y_top,-tz/2),( tx/2,y_top,-tz/2),( tx/2,y_top, tz/2),(-tx/2,y_top, tz/2),
    ]
    for x,y,z in points:
        verts.append((cx+x, y, cz+z))
    quads = [(1,2,3,4),(5,8,7,6),(1,5,6,2),(2,6,7,3),(3,7,8,4),(4,8,5,1)]
    for q in quads:
        faces.append((mat, tuple(start + i - 1 for i in q), name))

# Dimensions loosely match the image ratio: long 188, width 58, height 84.
# Unity model scale: length about 6m, width about 1.85m.

# Main frame and deck
add_box("main_deck", (0.75, 0.72, 0.0), (5.25, 0.22, 1.55), "beige")
add_box("front_frame", (3.45, 0.82, 0.0), (0.18, 0.34, 1.78), "beige")
add_box("rear_frame", (-2.45, 0.82, 0.0), (0.18, 0.34, 1.78), "beige")
add_box("left_side_frame", (0.55, 0.84, -0.86), (5.75, 0.32, 0.14), "beige")
add_box("right_side_frame", (0.55, 0.84, 0.86), (5.75, 0.32, 0.14), "beige")

# Seat slats: fewer than reference, enough to read visually.
for i in range(17):
    x = -1.75 + i * 0.29
    add_box(f"seat_slat_{i+1:02d}", (x, 0.99, 0.0), (0.055, 0.075, 1.43), "slat_dark")

# Tilted backrest frame, roughly 52 degrees.
angle = -52
add_box("back_panel_base", (-2.35, 1.46, 0.0), (2.05, 0.20, 1.50), "beige", angle)
add_box("back_left_rail", (-2.35, 1.46, -0.78), (2.15, 0.24, 0.13), "beige", angle)
add_box("back_right_rail", (-2.35, 1.46, 0.78), (2.15, 0.24, 0.13), "beige", angle)
add_box("back_top_rail", (-3.02, 2.28, 0.0), (0.16, 0.22, 1.55), "beige", angle)
add_box("back_bottom_rail", (-1.68, 0.66, 0.0), (0.16, 0.22, 1.55), "beige", angle)

for i in range(8):
    local = -0.78 + i * 0.22
    cx = -2.35 + local * math.cos(math.radians(angle))
    cy = 1.46 + local * math.sin(math.radians(angle))
    add_box(f"back_slat_{i+1:02d}", (cx, cy, 0.0), (0.045, 0.07, 1.30), "slat_dark", angle)

# Legs, tapered like the reference.
for name, x in [("rear", -2.35), ("middle", 0.25), ("front", 3.18)]:
    add_wedge_leg(f"{name}_leg_left", (x, 0.29, -0.67), (0.24,0.23), (0.17,0.17), 0.68, "beige")
    add_wedge_leg(f"{name}_leg_right", (x, 0.29, 0.67), (0.24,0.23), (0.17,0.17), 0.68, "beige")

# Back support arms and small dark hinge details.
add_box("back_support_left", (-1.83, 0.96, -0.67), (1.25, 0.10, 0.10), "beige", -47)
add_box("back_support_right", (-1.83, 0.96, 0.67), (1.25, 0.10, 0.10), "beige", -47)
add_box("left_hinge", (-1.15, 0.67, -0.72), (0.22, 0.10, 0.10), "dark")
add_box("right_hinge", (-1.15, 0.67, 0.72), (0.22, 0.10, 0.10), "dark")

with mtl_path.open("w", encoding="utf-8") as f:
    f.write("newmtl beige\n")
    f.write("Kd 0.62 0.58 0.51\n")
    f.write("Ka 0.62 0.58 0.51\n")
    f.write("Ks 0.08 0.08 0.08\n")
    f.write("Ns 20\n\n")
    f.write("newmtl slat_dark\n")
    f.write("Kd 0.28 0.27 0.24\n")
    f.write("Ka 0.28 0.27 0.24\n")
    f.write("Ks 0.03 0.03 0.03\n")
    f.write("Ns 5\n\n")
    f.write("newmtl dark\n")
    f.write("Kd 0.08 0.07 0.06\n")
    f.write("Ka 0.08 0.07 0.06\n")
    f.write("Ks 0.02 0.02 0.02\n")
    f.write("Ns 5\n")

with obj_path.open("w", encoding="utf-8") as f:
    f.write("# Low poly sun lounger generated for Unity\n")
    f.write("mtllib SunLounger_LowPoly.mtl\n")
    for v in verts:
        f.write(f"v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n")
    last_mat = None
    last_obj = None
    for mat, idxs, name in faces:
        if name != last_obj:
            f.write(f"o {name}\n")
            last_obj = name
            last_mat = None
        if mat != last_mat:
            f.write(f"usemtl {mat}\n")
            last_mat = mat
        f.write("f " + " ".join(str(i) for i in idxs) + "\n")

print(obj_path)
print(mtl_path)
print(f"vertices={len(verts)} faces={len(faces)} objects={len(set(name for _,_,name in faces))}")