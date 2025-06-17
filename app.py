from measure_by_img import *
import tkinter as tk
from PIL import Image, ImageTk
from tkinter import filedialog, messagebox

class FingerMeasurementApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Измерение фаланг пальцев")
        self.root.geometry("1000x700")

        # Переменные для данных
        self.image_path = None
        self.phalanges_data = None
        self.phalanges_data_abs = None
        self.finger_var = tk.StringVar(value="Указательный")  # Выбранный палец по умолчанию
        self.fingers_rus_names = {'thumb': 'Большой', 'index': 'Указательный', 'middle': 'Средний', 'ring': 'Безымянный', 'pinky': 'Мизинец'}
        
        # Параметры для сохранения
        self.rad1_var = tk.IntVar(value=10)
        self.rad2_var = tk.IntVar(value=10)
        self.rad3_var = tk.IntVar(value=10)
        self.r_joint_var = tk.IntVar(value=15)
        self.angle1_var = tk.IntVar(value=0)
        self.angle2_var = tk.IntVar(value=0)
        self.angle3_var = tk.IntVar(value=0)

        # Создание интерфейса
        self.create_widgets()

    def create_widgets(self):
        # Фрейм для загрузки изображения
        load_frame = tk.Frame(self.root)
        load_frame.pack(pady=10)

        tk.Button(
            load_frame, 
            text="Загрузить изображение", 
            command=self.load_image
        ).pack(side=tk.LEFT, padx=5)

        # Фрейм для отображения изображения и результатов
        display_frame = tk.Frame(self.root)
        display_frame.pack(fill=tk.BOTH, expand=True)

        # Изображение
        self.image_label = tk.Label(display_frame)
        self.image_label.pack(side=tk.LEFT, padx=10, pady=10)

        # Результаты измерений
        result_frame = tk.Frame(display_frame)
        result_frame.pack(side=tk.RIGHT, fill=tk.Y, padx=10, pady=10)

        tk.Label(result_frame, text="Результаты измерений", font=('Arial', 12, 'bold')).pack(pady=5)
        
        self.result_text = tk.Text(result_frame, height=15, width=40)
        self.result_text.pack()

        # Параметры сохранения
        param_frame = tk.Frame(result_frame)
        param_frame.pack(pady=10)

        tk.Label(param_frame, text="Параметры сохранения:").grid(row=0, columnspan=2, sticky='w')
        
        # Выбор пальца
        tk.Label(param_frame, text="Палец:").grid(row=1, column=0, sticky='w')
        finger_menu = tk.OptionMenu(
            param_frame, 
            self.finger_var, 
            *['Большой', 'Указательный', 'Средний', 'Безымянный', 'Мизинец']
        )
        finger_menu.grid(row=1, column=1, sticky='ew')

        # Радиусы фаланг
        tk.Label(param_frame, text="Радиус проксимальная фаланги:").grid(row=2, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.rad1_var, width=5).grid(row=2, column=1, sticky='w')
        
        tk.Label(param_frame, text="Радиус средней фаланги:").grid(row=3, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.rad2_var, width=5).grid(row=3, column=1, sticky='w')
        
        tk.Label(param_frame, text="Радиус дистальной фаланги:").grid(row=4, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.rad3_var, width=5).grid(row=4, column=1, sticky='w')
        
        tk.Label(param_frame, text="Радиус пястного сустава:").grid(row=5, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.r_joint_var, width=5).grid(row=5, column=1, sticky='w')

        # Углы
        tk.Label(param_frame, text="Угол между проксимальной и средней фалангой:").grid(row=6, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.angle1_var, width=5).grid(row=6, column=1, sticky='w')
        
        tk.Label(param_frame, text="Угол между средней и дистальной фалангой:").grid(row=7, column=0, sticky='w')
        tk.Entry(param_frame, textvariable=self.angle2_var, width=5).grid(row=7, column=1, sticky='w')
        

        # Кнопка сохранения
        tk.Button(
            result_frame, 
            text="Сохранить конфигурацию", 
            command=self.save_config
        ).pack(pady=10)

    def load_image(self):
        """Загрузка изображения и обработка"""
        file_path = filedialog.askopenfilename(
            filetypes=[("Image files", "*.jpg *.jpeg *.png")]
        )
        
        if not file_path:
            return

        self.image_path = file_path
        
        # Обработка изображения
        self.phalanges_data, self.phalanges_data_abs, annotated_image = detect_finger_phalanges(file_path)

        # Отображение результатов
        photo = ImageTk.PhotoImage(annotated_image)
        
        self.image_label.config(image=photo)
        self.image_label.image = photo
        
        self.show_results()

    def show_results(self):
        """Вывод результатов измерений"""
        if not self.phalanges_data:
            return

        self.result_text.delete(1.0, tk.END)
        
        self.result_text.insert(tk.END, "Относительный размер\n")
        for finger, lengths in self.phalanges_data.items():
            self.result_text.insert(tk.END, f"{self.fingers_rus_names[finger]}:\n")
            for i, length in enumerate(lengths, 1):
                self.result_text.insert(tk.END, f"  Фаланга {i}: {length:.4f}\n")
            self.result_text.insert(tk.END, "\n")
        self.result_text.insert(tk.END, "Абсолютный размер (мм)\n")
        for finger, lengths in self.phalanges_data_abs.items():
            self.result_text.insert(tk.END, f"{self.fingers_rus_names[finger]}:\n")
            for i, length in enumerate(lengths, 1):
                self.result_text.insert(tk.END, f"  Фаланга {i}: {length:.4f}\n")
            self.result_text.insert(tk.END, "\n")

    def save_config(self):
        """Сохранение конфигурации в файл"""
        if not self.phalanges_data:
            messagebox.showerror("Ошибка", "Сначала загрузите изображение")
            return

        # Получаем данные для выбранного пальца
        finger_rus_chosen = self.finger_var.get()
        finger = None
        for finger_eng, finger_rus in self.fingers_rus_names.items():
            if finger_rus == finger_rus_chosen:
                finger = finger_eng
                break
        
        lengths = self.phalanges_data_abs.get(finger)
        
        if not lengths:
            messagebox.showerror("Ошибка", "Нет данных для выбранного пальца")
            return
        
        #проверяем данные
        try:
            r1 = float(self.rad1_var.get())
            r2 = float(self.rad2_var.get())
            r3 = float(self.rad3_var.get())
            r_joint = float(self.r_joint_var.get())
            angle1 = float(self.angle1_var.get())
            angle2 = float(self.angle2_var.get())
            if r1 <= 0 or r2 <=0 or r3 <= 0 or angle1 < 0 or angle1 > 110 or angle2 < 0 or angle2 > 90:
                raise ValueError
        except ValueError:
            return
        
        # Создаем конфигурацию
        create_finger_config_file(lengths, finger, (r1,r2,r3), r_joint, (angle1, angle2))


if __name__ == "__main__":
    root = tk.Tk()
    app = FingerMeasurementApp(root)
    root.mainloop()