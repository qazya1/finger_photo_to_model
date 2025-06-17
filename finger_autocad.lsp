(defun load_parameters_from_file (filename / file line param value pos)
    (setq default_params '( ; значения по умолчанию
        ("L1" . 55.0)
        ("L2" . 25.0)
        ("L3" . 20.0)
        ("R1" . 10.0)
        ("R2" . 10.0)
        ("R3" . 10.0)
        ("R_joint" . 15.0)
        ("FlexAngle1" . 30.0)
        ("FlexAngle2" . 10.0)
    ))
    
    ; Установка значений по умолчанию
    (foreach param default_params
        (set (read (car param)) (cdr param))
    )
    
    (if (setq file (findfile filename))
        (progn
            (setq file (open file "r"))
            (while (setq line (read-line file))
                (setq line (vl-string-trim " \t\r\n" line))
                ; Пропускаем пустые строки и комментарии
                (if (and (> (strlen line) 0) (/= (substr line 1 1) ";"))
                    (progn
                        (setq pos (vl-string-position (ascii "=") line))
                        (if pos
                            (progn
                                (setq param (vl-string-trim " " (substr line 1 pos)))
                                (setq value (vl-string-trim " " (substr line (+ pos 2))))
                                (if (and param value (numberp (read value)))
                                    (set (read param) (atof value))
                                    (princ (strcat "\nОшибка в строке: " line))
                                )
                            )
                        )
                    )
                )
            )
            (close file)
            (princ (strcat "\nПараметры успешно загружены из файла: " filename))
            T ; возвращаем T при успешной загрузке
        )
        (progn
            (princ (strcat "\nФайл конфигурации " filename " не найден. Используются значения по умолчанию."))
            nil ; возвращаем nil при отсутствии файла
        )
    )
)

(defun c:create_finger_model ()
    ;загрузка значений из файла
    (setq filename (getfiled "выберите файл конфигурации" "" "" 16))
    (setq config_file filename)
    (load_parameters_from_file config_file)
    
    (setq D1 (* R1 2))     ; Диаметр проксимальной фаланги
    (setq D2 (* R2 2))     ; Диаметр средней фаланги
    
    ; сброс к мировой системе координат
    (command "_ucs" "_world")

    ; === 1. создание проксимальной фаланги ===
    (command "_cylinder" "_non" "0,0,0" R1 L1)
    (setq Phalanx1 (entlast))
    (command "_sphere" "_non" "0,0,0" R_joint)

    ; === 2. создание сустава ===
    (command "_ucs" "_o" "_non" (list 0 0 L1))

    (command "_cylinder" "_non" "0,0,0" R1 D1)
    (setq cil1Help_1 (entlast))

    (command "_ucs" "_y" 90)
    (command "_cylinder" "_non" (list 0 0 (- R1)) D1 D1)
    (setq cil2Help_1 (entlast))

    (command "_intersect" cil1Help_1 cil2Help_1 "")
    (setq kreplenye (entlast))

    (command "_ucs" "_y" -90)

    (command "_box" 
        "_non" (list (- (/ R1 2)) (- R1) 0) 
        "_non" (list (/ R1 2) R1 D1))
    (setq vyrez (entlast))

    (command "_subtract" kreplenye "" vyrez "")

    (command "_ucs" "_o" "_non" (list 0 0 R1))

    (command "_ucs" "_y" 90)

    (command "_cylinder" "_non" (list 0 0 (- R1)) (/ R1 2) D1)

    (command "_ucs" "_y" -90)

    (command "_ucs" "_x" FlexAngle1)

    (command "_ucs" "_o" "_non" (list 0 0 (/ R1 2)))

    (command "_box" 
        "_non" (list (- (/ R1 2)) (- (* R1 0.5)) R1) 
        "_non" (list (/ R1 2) R2 (- R1)))
    (setq perehodn1 (entlast))

    (command "_ucs" "_o" "_non" (list 0 0 R1))

    (command "_cylinder" "_non" (list 0 0 0) R2 (- D1))
    (setq cil2Help_2 (entlast))

    (command "_intersect" cil2Help_2 perehodn1 "")

    (if (> L2 0)    ;проверка (для большого пальца)
        ( progn 
        ; === 3. создание средней фаланги ===
        (command "_cylinder" "_non" "0,0,0" R2 L2)
        (setq Phalanx2 (entlast))

        ; === 4. создание сустава ===
        (command "_ucs" "_o" "_non" (list 0 0 L2))

        (command "_cylinder" "_non" "0,0,0" R2 D2)
        (setq cil1Help_3 (entlast))

        (command "_ucs" "_y" 90)
        (command "_cylinder" "_non" (list 0 0 (- R2)) D2 D2)
        (setq cil2Help_3 (entlast))

        (command "_intersect" cil1Help_3 cil2Help_3 "")
        (setq kreplenye1 (entlast))

        (command "_ucs" "_y" -90)
        
        (command "_box" 
            "_non" (list (- (/ R2 2)) (- R2) 0) 
            "_non" (list (/ R2 2) R2 D2))
        (setq vyrez1 (entlast))

        (command "_subtract" kreplenye1 "" vyrez1 "")

        (command "_ucs" "_o" "_non" (list 0 0 R2))

        (command "_ucs" "_y" 90)

        (command "_cylinder" "_non" (list 0 0 (- R2)) (/ R2 2) D2)

        (command "_ucs" "_y" -90)

        (command "_ucs" "_x" FlexAngle2)

        (command "_ucs" "_o" "_non" (list 0 0 (/ R2 2)))

        (princ (list (- (/ R2 2)) (- R2) R2) )
        (princ (list (/ R2 2) R2 R2))
        (command "_box" 
            "_non" (list (- (/ R2 2)) (- R2) R2) 
            "_non" (list (/ R2 2) R2 (- R2)))
        (setq perehodn2 (entlast))

        (command "_ucs" "_o" "_non" (list 0 0 R2))

        (command "_cylinder" "_non" (list 0 0 0) R2 (- R2))
        (setq cil2Help_4 (entlast))

        (command "_intersect" cil2Help_4 perehodn2 "")
        )
    )
    
    ; === 3. создание дистальной фаланги ===
    (command "_cylinder" "_non" "0,0,0" R3 L3)
    (setq Phalanx3 (entlast))
    (command "_sphere" "_non" (list 0 0 L3) R3)

    ; сброс к мировой системе координат
    (command "_ucs" "_world")
)