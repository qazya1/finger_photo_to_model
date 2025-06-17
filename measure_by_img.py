import cv2
import mediapipe as mp
import numpy as np
from math import sqrt
from PIL import Image

mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
hands = mp_hands.Hands(static_image_mode=True, max_num_hands=1, min_detection_confidence=0.5)

def calculate_distance(point1, point2):
    """Вычисляет расстояние между двумя точками"""
    return sqrt((point1.x - point2.x)**2 + (point1.y - point2.y)**2)

def detect_finger_phalanges(image_path):
    """Определяет длины фаланг пальцев на изображении"""
    # Чтение изображения
    image = cv2.imread(image_path)
    image = cv2.resize(image, (600, 800))
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    
    # Обработка изображения
    results = hands.process(image_rgb)
    
    if not results.multi_hand_landmarks:
        print("Рука не обнаружена")
        return None
    
    # Словарь для хранения длин фаланг
    phalanges = {
        'thumb': [],     #большой палец
        'index': [],    #указательный палец
        'middle': [],    #средний палец
        'ring': [],    #безымянный палец
        'pinky': []    #мизинец
    }
    
    # Получаем ключевые точки руки
    hand_landmarks = results.multi_hand_landmarks[0]
    
    # Определяем индексы суставов для каждой фаланги
    finger_joints = {
        'thumb': [1, 2, 3, 4],
        'index': [5, 6, 7, 8],
        'middle': [9, 10, 11, 12],
        'ring': [13, 14, 15, 16],
        'pinky': [17, 18, 19, 20]
    }
    
    # Вычисляем длины фаланг для каждого пальца
    for finger, joints in finger_joints.items():
        for i in range(len(joints)-1):
            # Получаем точки суставов
            point1 = hand_landmarks.landmark[joints[i]]
            point2 = hand_landmarks.landmark[joints[i+1]]
            
            # Вычисляем расстояние между точками (в относительных координатах)
            distance = calculate_distance(point1, point2)
            
            # Сохраняем длину фаланги
            phalanges[finger].append(distance)
    
    #отдельно обрабатываем большой палец
    phalanges['thumb'][0] = phalanges['thumb'][1]
    phalanges['thumb'][1] = 0
    
    # Визуализация
    annotated_image = image.copy()
    mp_drawing.draw_landmarks(
        annotated_image, hand_landmarks, mp_hands.HAND_CONNECTIONS)
    
    # Отображение результатов
    #cv2.imshow("Hand with landmarks", annotated_image)
    #cv2.waitKey(0)
    #cv2.destroyAllWindows()
    
    phalanges_abs = {}
    for finger in phalanges:
        phalanges_abs[finger] = [length*226 for length in phalanges[finger]]
    
    return phalanges, phalanges_abs, Image.fromarray(annotated_image)

def create_finger_config_file(lengths, finger_name, rads=(10,10,10), r_joint=15, angles=(0,0)):
    with open(f"{finger_name}_finger_config.txt", "w", encoding="utf-8") as config_file:
        config_file.write(f"L1={lengths[0]}\n")
        config_file.write(f"L2={lengths[1]}\n")
        config_file.write(f"L3={lengths[2]}\n")
        config_file.write(f"R1={rads[0]}\n")
        config_file.write(f"R2={rads[1]}\n")
        config_file.write(f"R3={rads[2]}\n")
        config_file.write(f"R_joint={r_joint}\n")
        config_file.write(f"FlexAngle1={angles[0]}\n")
        config_file.write(f"FlexAngle2={angles[1]}")

if __name__ == "__main__":
    image_path = input()
    phalanges_lengths = detect_finger_phalanges(image_path)

    if phalanges_lengths:
        print("Длины фаланг (в относительных единицах):")
        for finger, lengths in phalanges_lengths.items():
            print(f"{finger}: {lengths}")
            print(f"{finger}: {[length*226 for length in lengths]}")
            create_finger_config_file([length*226 for length in lengths], finger)