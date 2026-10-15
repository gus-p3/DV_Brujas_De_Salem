import os
from PIL import Image, ImageDraw

def create_villager_art():
    output_dir = "Assets/Art/Characters/Villager"
    os.makedirs(output_dir, exist_ok=True)

    cell_w, cell_h = 256, 256

    def draw_outline_circle(draw, xy, fill, outline, width=3):
        x0, y0, x1, y1 = xy
        draw.ellipse([x0, y0, x1, y1], fill=fill, outline=outline, width=width)

    def draw_outline_rect(draw, xy, fill, outline, width=3):
        draw.rectangle(xy, fill=fill, outline=outline, width=width)

    def draw_outline_polygon(draw, points, fill, outline, width=3):
        draw.polygon(points, fill=fill, outline=outline)
        # Dibujar líneas gruesas de contorno
        for i in range(len(points)):
            p1 = points[i]
            p2 = points[(i + 1) % len(points)]
            draw.line([p1, p2], fill=outline, width=width)

    # ------------------ Aldeano Masculino (Colonial Puritan) ------------------
    # Colores:
    outline = (28, 20, 24, 255)
    shadow_col = (40, 30, 35, 60)
    skin = (245, 208, 178, 255)
    skin_shadow = (220, 180, 150, 255)
    clothes_coat = (95, 75, 60, 255) # Chaqueta marrón colonial
    clothes_pants = (45, 40, 48, 255) # Pantalón oscuro
    collar_white = (240, 240, 235, 255)
    hat_col = (35, 30, 35, 255)
    hat_buckle = (230, 195, 50, 255)
    shoes_col = (25, 20, 22, 255)

    def draw_villager_m(draw, offset_x, anim_type, frame_idx):
        cx = offset_x + 128
        feet_y = 236

        # Sombra plana en el suelo
        sw = 48 if anim_type != 'run' else 56
        draw.ellipse([cx - sw, feet_y - 6, cx + sw, feet_y + 6], fill=shadow_col)

        # Variaciones por animación
        bob_y = 0
        leg_l_angle = 0
        leg_r_angle = 0
        arm_l_state = 'down'
        arm_r_state = 'down'
        head_tilt = 0
        mouth_open = False
        eyes_wide = False

        if anim_type == 'idle':
            bob_y = [0, -2, -3, -1][frame_idx % 4]
        elif anim_type == 'walk':
            bob_y = [-2, 0, -2, 0][frame_idx % 4]
            leg_l_angle = [-14, 0, 14, 0][frame_idx % 4]
            leg_r_angle = [14, 0, -14, 0][frame_idx % 4]
            arm_l_state = 'swing_back' if frame_idx in [0, 1] else 'swing_front'
            arm_r_state = 'swing_front' if frame_idx in [0, 1] else 'swing_back'
        elif anim_type == 'scream':
            bob_y = [-3, -5, -4, -1][frame_idx % 4]
            arm_l_state = 'raised_up'
            arm_r_state = 'raised_up'
            mouth_open = True
            eyes_wide = True
            head_tilt = [-2, 2, -2, 2][frame_idx % 4]
        elif anim_type == 'run':
            bob_y = [-6, 0, -6, 0][frame_idx % 4]
            leg_l_angle = [-28, 4, 28, -4][frame_idx % 4]
            leg_r_angle = [28, -4, -28, 4][frame_idx % 4]
            arm_l_state = 'run_swing_back' if frame_idx in [0, 1] else 'run_swing_front'
            arm_r_state = 'run_swing_front' if frame_idx in [0, 1] else 'run_swing_back'
            eyes_wide = True

        root_y = feet_y + bob_y

        # Piernas y botas
        # Pierna Izquierda
        p1_x = cx - 14 + leg_l_angle
        draw_outline_rect(draw, [p1_x - 7, root_y - 50, p1_x + 7, root_y - 12], fill=clothes_pants, outline=outline, width=3)
        draw_outline_rect(draw, [p1_x - 10, root_y - 14, p1_x + 9, root_y], fill=shoes_col, outline=outline, width=3)

        # Pierna Derecha
        p2_x = cx + 14 + leg_r_angle
        draw_outline_rect(draw, [p2_x - 7, root_y - 50, p2_x + 7, root_y - 12], fill=clothes_pants, outline=outline, width=3)
        draw_outline_rect(draw, [p2_x - 9, root_y - 14, p2_x + 10, root_y], fill=shoes_col, outline=outline, width=3)

        # Torso / Chaqueta
        coat_pts = [
            (cx - 26, root_y - 110),
            (cx + 26, root_y - 110),
            (cx + 30, root_y - 50),
            (cx - 30, root_y - 50)
        ]
        draw_outline_polygon(draw, coat_pts, fill=clothes_coat, outline=outline, width=4)

        # Cinturón colonial con hebilla
        draw_outline_rect(draw, [cx - 28, root_y - 62, cx + 28, root_y - 52], fill=(20, 18, 20, 255), outline=outline, width=2)
        draw_outline_rect(draw, [cx - 8, root_y - 65, cx + 8, root_y - 49], fill=hat_buckle, outline=outline, width=2)

        # Cuello blanco puritano (pechera)
        collar_pts = [
            (cx - 18, root_y - 112),
            (cx, root_y - 92),
            (cx + 18, root_y - 112),
            (cx, root_y - 105)
        ]
        draw_outline_polygon(draw, collar_pts, fill=collar_white, outline=outline, width=3)

        # Cabeza
        head_cy = root_y - 130
        draw_outline_circle(draw, [cx - 18 + head_tilt, head_cy - 18, cx + 18 + head_tilt, head_cy + 18], fill=skin, outline=outline, width=3)

        # Ojos
        eye_y = head_cy - 2
        eye_sz = 4 if not eyes_wide else 6
        draw_outline_circle(draw, [cx - 9 - eye_sz//2 + head_tilt, eye_y - eye_sz//2, cx - 9 + eye_sz//2 + head_tilt, eye_y + eye_sz//2], fill=(255,255,255,255), outline=outline, width=2)
        draw.ellipse([cx - 9 - 1 + head_tilt, eye_y - 1, cx - 9 + 1 + head_tilt, eye_y + 1], fill=outline)

        draw_outline_circle(draw, [cx + 9 - eye_sz//2 + head_tilt, eye_y - eye_sz//2, cx + 9 + eye_sz//2 + head_tilt, eye_y + eye_sz//2], fill=(255,255,255,255), outline=outline, width=2)
        draw.ellipse([cx + 9 - 1 + head_tilt, eye_y - 1, cx + 9 + 1 + head_tilt, eye_y + 1], fill=outline)

        # Boca
        if mouth_open:
            draw_outline_circle(draw, [cx - 6 + head_tilt, head_cy + 6, cx + 6 + head_tilt, head_cy + 14], fill=(160, 40, 50, 255), outline=outline, width=2)
        else:
            draw.line([cx - 5 + head_tilt, head_cy + 8, cx + 5 + head_tilt, head_cy + 8], fill=outline, width=3)

        # Sombrero de peregrino / puritano con hebilla
        hat_base_y = head_cy - 14
        # Ala del sombrero
        draw_outline_rect(draw, [cx - 32 + head_tilt, hat_base_y - 4, cx + 32 + head_tilt, hat_base_y + 4], fill=hat_col, outline=outline, width=3)
        # Copa del sombrero
        crown_pts = [
            (cx - 20 + head_tilt, hat_base_y - 4),
            (cx + 20 + head_tilt, hat_base_y - 4),
            (cx + 15 + head_tilt, hat_base_y - 34),
            (cx - 15 + head_tilt, hat_base_y - 34)
        ]
        draw_outline_polygon(draw, crown_pts, fill=hat_col, outline=outline, width=3)
        # Cinta y hebilla del sombrero
        draw.rectangle([cx - 19 + head_tilt, hat_base_y - 12, cx + 19 + head_tilt, hat_base_y - 4], fill=(18, 16, 20, 255))
        draw_outline_rect(draw, [cx - 7 + head_tilt, hat_base_y - 14, cx + 7 + head_tilt, hat_base_y - 2], fill=hat_buckle, outline=outline, width=2)

        # Brazos
        if arm_l_state == 'raised_up':
            # Brazo izquierdo arriba alertando
            pts_l = [(cx - 24, root_y - 105), (cx - 42, root_y - 135), (cx - 36, root_y - 142), (cx - 18, root_y - 105)]
            draw_outline_polygon(draw, pts_l, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 45, root_y - 152, cx - 33, root_y - 140], fill=skin, outline=outline, width=2)
            # Brazo derecho arriba
            pts_r = [(cx + 24, root_y - 105), (cx + 42, root_y - 135), (cx + 36, root_y - 142), (cx + 18, root_y - 105)]
            draw_outline_polygon(draw, pts_r, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 33, root_y - 152, cx + 45, root_y - 140], fill=skin, outline=outline, width=2)
        elif arm_l_state == 'run_swing_back':
            pts_l = [(cx - 22, root_y - 105), (cx - 42, root_y - 75), (cx - 34, root_y - 70), (cx - 16, root_y - 102)]
            draw_outline_polygon(draw, pts_l, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 46, root_y - 74, cx - 34, root_y - 62], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 20, root_y - 105), (cx + 38, root_y - 120), (cx + 44, root_y - 114), (cx + 24, root_y - 100)]
            draw_outline_polygon(draw, pts_r, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 40, root_y - 128, cx + 52, root_y - 116], fill=skin, outline=outline, width=2)
        elif arm_l_state == 'run_swing_front':
            pts_l = [(cx - 20, root_y - 105), (cx - 38, root_y - 120), (cx - 44, root_y - 114), (cx - 24, root_y - 100)]
            draw_outline_polygon(draw, pts_l, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 52, root_y - 128, cx - 40, root_y - 116], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 22, root_y - 105), (cx + 42, root_y - 75), (cx + 34, root_y - 70), (cx + 16, root_y - 102)]
            draw_outline_polygon(draw, pts_r, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 34, root_y - 74, cx + 46, root_y - 62], fill=skin, outline=outline, width=2)
        elif arm_l_state == 'swing_back':
            pts_l = [(cx - 24, root_y - 105), (cx - 34, root_y - 70), (cx - 28, root_y - 68), (cx - 18, root_y - 103)]
            draw_outline_polygon(draw, pts_l, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 36, root_y - 68, cx - 26, root_y - 58], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 24, root_y - 105), (cx + 34, root_y - 78), (cx + 28, root_y - 76), (cx + 18, root_y - 103)]
            draw_outline_polygon(draw, pts_r, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 28, root_y - 78, cx + 38, root_y - 68], fill=skin, outline=outline, width=2)
        else: # Idle / swing_front
            pts_l = [(cx - 24, root_y - 105), (cx - 30, root_y - 65), (cx - 22, root_y - 65), (cx - 16, root_y - 105)]
            draw_outline_polygon(draw, pts_l, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 32, root_y - 65, cx - 20, root_y - 53], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 24, root_y - 105), (cx + 30, root_y - 65), (cx + 22, root_y - 65), (cx + 16, root_y - 105)]
            draw_outline_polygon(draw, pts_r, fill=clothes_coat, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 20, root_y - 65, cx + 32, root_y - 53], fill=skin, outline=outline, width=2)

    # Generar las 4 animaciones del aldeano masculino
    for anim, frames in [('idle', 4), ('walk', 4), ('scream', 4), ('run', 4)]:
        img = Image.new('RGBA', (cell_w * frames, cell_h), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        for i in range(frames):
            draw_villager_m(draw, i * cell_w, anim, i)
        fname = f"Villager_{anim.capitalize()}.png"
        path = os.path.join(output_dir, fname)
        img.save(path)
        print(f"Generado {path} ({cell_w*frames}x{cell_h})")

    # ------------------ Aldeana Femenina (Colonial Puritan Bonnet & Dress) ------------------
    dress_col = (55, 68, 88, 255) # Azul oscuro/gris colonial
    apron_col = (235, 235, 230, 255) # Delantal blanco colonial
    bonnet_col = (245, 245, 240, 255) # Cofia puritana blanca

    def draw_villager_f(draw, offset_x, anim_type, frame_idx):
        cx = offset_x + 128
        feet_y = 236

        sw = 46 if anim_type != 'run' else 54
        draw.ellipse([cx - sw, feet_y - 6, cx + sw, feet_y + 6], fill=shadow_col)

        bob_y = 0
        leg_l_angle = 0
        leg_r_angle = 0
        mouth_open = False
        eyes_wide = False
        arms_up = False

        if anim_type == 'idle':
            bob_y = [0, -2, -3, -1][frame_idx % 4]
        elif anim_type == 'walk':
            bob_y = [-2, 0, -2, 0][frame_idx % 4]
            leg_l_angle = [-10, 0, 10, 0][frame_idx % 4]
            leg_r_angle = [10, 0, -10, 0][frame_idx % 4]
        elif anim_type == 'scream':
            bob_y = [-3, -5, -4, -1][frame_idx % 4]
            mouth_open = True
            eyes_wide = True
            arms_up = True
        elif anim_type == 'run':
            bob_y = [-6, 0, -6, 0][frame_idx % 4]
            leg_l_angle = [-24, 4, 24, -4][frame_idx % 4]
            leg_r_angle = [24, -4, -24, 4][frame_idx % 4]
            eyes_wide = True

        root_y = feet_y + bob_y

        # Zapatos asomando bajo la falda larga
        p1_x = cx - 12 + leg_l_angle
        p2_x = cx + 12 + leg_r_angle
        draw_outline_rect(draw, [p1_x - 7, root_y - 12, p1_x + 7, root_y], fill=shoes_col, outline=outline, width=3)
        draw_outline_rect(draw, [p2_x - 7, root_y - 12, p2_x + 7, root_y], fill=shoes_col, outline=outline, width=3)

        # Falda larga acampanada colonial
        skirt_pts = [
            (cx - 18, root_y - 80),
            (cx + 18, root_y - 80),
            (cx + 34, root_y - 8),
            (cx - 34, root_y - 8)
        ]
        draw_outline_polygon(draw, skirt_pts, fill=dress_col, outline=outline, width=4)

        # Delantal blanco sobre la falda
        apron_pts = [
            (cx - 14, root_y - 80),
            (cx + 14, root_y - 80),
            (cx + 20, root_y - 20),
            (cx - 20, root_y - 20)
        ]
        draw_outline_polygon(draw, apron_pts, fill=apron_col, outline=outline, width=3)

        # Corpiño / Torso
        torso_pts = [
            (cx - 20, root_y - 110),
            (cx + 20, root_y - 110),
            (cx + 18, root_y - 80),
            (cx - 18, root_y - 80)
        ]
        draw_outline_polygon(draw, torso_pts, fill=dress_col, outline=outline, width=3)

        # Cuello blanco puritano
        draw_outline_rect(draw, [cx - 16, root_y - 112, cx + 16, root_y - 102], fill=collar_white, outline=outline, width=2)

        # Cabeza
        head_cy = root_y - 126
        # Cofia Puritana (Bonnet) alrededor y detrás de la cabeza
        bonnet_pts = [
            (cx - 24, head_cy - 16),
            (cx, head_cy - 28),
            (cx + 24, head_cy - 16),
            (cx + 22, head_cy + 18),
            (cx - 22, head_cy + 18)
        ]
        draw_outline_polygon(draw, bonnet_pts, fill=bonnet_col, outline=outline, width=3)

        # Rostro
        draw_outline_circle(draw, [cx - 15, head_cy - 15, cx + 15, head_cy + 15], fill=skin, outline=outline, width=3)

        # Ojos
        eye_y = head_cy - 2
        eye_sz = 4 if not eyes_wide else 6
        draw_outline_circle(draw, [cx - 7 - eye_sz//2, eye_y - eye_sz//2, cx - 7 + eye_sz//2, eye_y + eye_sz//2], fill=(255,255,255,255), outline=outline, width=2)
        draw.ellipse([cx - 7 - 1, eye_y - 1, cx - 7 + 1, eye_y + 1], fill=outline)
        draw_outline_circle(draw, [cx + 7 - eye_sz//2, eye_y - eye_sz//2, cx + 7 + eye_sz//2, eye_y + eye_sz//2], fill=(255,255,255,255), outline=outline, width=2)
        draw.ellipse([cx + 7 - 1, eye_y - 1, cx + 7 + 1, eye_y + 1], fill=outline)

        # Boca
        if mouth_open:
            draw_outline_circle(draw, [cx - 5, head_cy + 6, cx + 5, head_cy + 13], fill=(160, 40, 50, 255), outline=outline, width=2)
        else:
            draw.line([cx - 4, head_cy + 7, cx + 4, head_cy + 7], fill=outline, width=2)

        # Brazos
        if arms_up:
            pts_l = [(cx - 18, root_y - 105), (cx - 36, root_y - 135), (cx - 30, root_y - 140), (cx - 12, root_y - 105)]
            draw_outline_polygon(draw, pts_l, fill=dress_col, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 38, root_y - 148, cx - 28, root_y - 138], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 18, root_y - 105), (cx + 36, root_y - 135), (cx + 30, root_y - 140), (cx + 12, root_y - 105)]
            draw_outline_polygon(draw, pts_r, fill=dress_col, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 28, root_y - 148, cx + 38, root_y - 138], fill=skin, outline=outline, width=2)
        else:
            pts_l = [(cx - 18, root_y - 105), (cx - 24, root_y - 70), (cx - 16, root_y - 70), (cx - 10, root_y - 105)]
            draw_outline_polygon(draw, pts_l, fill=dress_col, outline=outline, width=3)
            draw_outline_circle(draw, [cx - 25, root_y - 70, cx - 15, root_y - 60], fill=skin, outline=outline, width=2)
            pts_r = [(cx + 18, root_y - 105), (cx + 24, root_y - 70), (cx + 16, root_y - 70), (cx + 10, root_y - 105)]
            draw_outline_polygon(draw, pts_r, fill=dress_col, outline=outline, width=3)
            draw_outline_circle(draw, [cx + 15, root_y - 70, cx + 25, root_y - 60], fill=skin, outline=outline, width=2)

    for anim, frames in [('idle', 4), ('walk', 4), ('scream', 4), ('run', 4)]:
        img = Image.new('RGBA', (cell_w * frames, cell_h), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        for i in range(frames):
            draw_villager_f(draw, i * cell_w, anim, i)
        fname = f"VillagerF_{anim.capitalize()}.png"
        path = os.path.join(output_dir, fname)
        img.save(path)
        print(f"Generado {path} ({cell_w*frames}x{cell_h})")

if __name__ == '__main__':
    create_villager_art()
