using System;
using Kompas6API5;
using Kompas6Constants3D;
using System.Runtime.InteropServices;

namespace Project1
{
    public class FingerModelCreator
    {
        private KompasObject _kompas;
        private ksDocument3D _document3D;
        private ksPart _part;
        private Dictionary<string, double> _parameters;

        public FingerModelCreator(Dictionary<string, double> parameters)
        {
            _parameters = parameters;
            
            // Получаем экземпляр КОМПАС-3D
            try
            {
                _kompas = (KompasObject)Marshal.GetActiveObject("KOMPAS.Application.5");
            }
            catch
            {
                _kompas = (KompasObject)Activator.CreateInstance(Type.GetTypeFromProgID("KOMPAS.Application.5"));
            }
            
            // Создаем новый документ детали
            _document3D = (ksDocument3D)_kompas.Document3D();
            _document3D.Create();
            _part = (ksPart)_document3D.GetPart((short)Part_Type.pTop_Part);
        }

        
        public void CreateFingerModel()
        {
            // Получаем параметры из словаря
            double R1 = _parameters["R1"];
            double R2 = _parameters["R2"];
            double R3 = _parameters["R3"];
            double L1 = _parameters["L1"];
            double L2 = _parameters["L2"];
            double L3 = _parameters["L3"];
            double R_joint = _parameters["R_joint"];
            double FlexAngle1 = _parameters["FlexAngle1"];
            double FlexAngle2 = _parameters["FlexAngle2"];
            
            double D1 = R1 * 2;     // Диаметр проксимальной фаланги
            double D2 = R2 * 2;     // Диаметр средней фаланги

            // === 1. Создание проксимальной фаланги ===
            ksEntity cylinder1 = CreateCylinder(_part.GetDefaultEntity((short)Obj3dType.o3d_planeXOY), R1, L1);
            ksEntity phalanx1 = cylinder1;

            // Создание сферы (сустава)
            ksEntity sphere1 = _part.NewEntity((short)Obj3dType.o3d_sphere);
            ksSphereParam sphere1Param = (ksSphereParam)sphere1.GetDefinition();
            sphere1Param.SetPlane(_part.GetDefaultEntity((short)Obj3dType.o3d_planeXOY));
            sphere1Param.radius = R_joint;
            sphere1.Create();
        
            // === 2. Создание сустава ===
            
            ksEntity plane1 = CreateJoint(_part.GetDefaultEntity((short)Obj3dType.o3d_planeXOY), R1, R2, D1, L1, FlexAngle1);
            
            if (L2 > 0) // Проверка для большого пальца
            {
                // === 3. Создание средней фаланги ===
                ksEntity cylinder4 = CreateCylinder(plane1, R2, L2);
                ksEntity phalanx2 = cylinder4;

                // === 4. Создание сустава ===
                ksEntity plane2 = CreateJoint(plane1, R2, R3, D2, L2, FlexAngle2);
            }
            
            // === 5. Создание дистальной фаланги ===
            ksEntity cylinder5 = _CreateCylinder(plane2, R3, L3);
            ksEntity phalanx3 = cylinder5;
            
            ksEntity plane3 = CreateOffsetPlane(plane2, L3);
            // Создание сферы на конце дистальной фаланги
            ksEntity sphere2 = _part.NewEntity((short)Obj3dType.o3d_sphere);
            ksSphereParam sphere2Param = (ksSphereParam)sphere2.GetDefinition();
            sphere2Param.SetPlane((ksEntity)plane3);
            sphere2Param.radius = R3;
            sphere2.Create();
        }

        // Вспомогательные методы:

        // === Создание сустава ===
        private ksEntity CreateJoint(ksEntity basePlane, double R1, double R2, double D1, double L1, double FlexAngle1)
        {
            // 1. Создаем плоскость смещения по Z на L1 от базовой плоскости
            ksEntity offsetPlaneL1 = CreateOffsetPlane(basePlane, L1);
            
            // 2. Создаем первый цилиндр (вертикальный)
            ksEntity cylinder1 = CreateCylinder(offsetPlaneL1, R1, D1);
            ksEntity cil1Help_1 = cylinder1;
            
            // 3. Создаем плоскость повернутую на 90° вокруг оси Y
            ksEntity rotatedPlaneY90 = CreateRotatedPlane(offsetPlaneL1, 90, (short)Direction_Type.dtNormal);
            
            // 4. Создаем второй цилиндр (горизонтальный)
            ksEntity cylinder2 = CreateCylinder(rotatedPlaneY90, D1, D1, new double[] {0, 0, -R1});
            ksEntity cil2Help_1 = cylinder2;
            
            // 5. Операция пересечения цилиндров
            ksEntity kreplenye = CreateIntersection(cil1Help_1, cil2Help_1);
            
            // 7. Создаем вырез (параллелепипед)
            ksEntity box1 = CreateBox(offsetPlaneL1, 
                                     new double[] {-R1/2, -R1, 0}, 
                                     new double[] {R1/2, R1, D1});
            ksEntity vyrez = box1;
            
            // 8. Операция вычитания (вырезаем параллелепипед из пересечения)
            CreateSubtraction(kreplenye, vyrez);
            
            // 9. Создаем новую плоскость смещения по Z на R1 от предыдущей
            ksEntity offsetPlaneR1 = CreateOffsetPlane(offsetPlaneL1, R1);
            
            // 10. Создаем плоскость повернутую на 90° вокруг оси Y
            ksEntity rotatedPlaneY90_2 = CreateRotatedPlane(offsetPlaneR1, 90, (short)Direction_Type.dtNormal);
            
            // 11. Создаем третий цилиндр (горизонтальный)
            ksEntity cylinder3 = CreateCylinder(rotatedPlaneY90_2, R1/2, D1, new double[] {0, 0, -R1});
            
            // 13. Создаем плоскость повернутую на FlexAngle1 вокруг оси X
            ksEntity rotatedPlaneFlex = CreateRotatedPlane(offsetPlaneR1, FlexAngle1, (short)Direction_Type.dtNormal, axis: "X");
            
            // 14. Создаем плоскость смещения по Z на R1/2 от повернутой плоскости
            ksEntity offsetPlaneFlex = CreateOffsetPlane(rotatedPlaneFlex, R1/2);
            
            // 15. Создаем переходный элемент (параллелепипед)
            ksEntity box2 = CreateBox(offsetPlaneFlex, 
                                     new double[] {-R1/2, -R1*0.5, R1}, 
                                     new double[] {R1/2, R2, -R1});
            ksEntity perehodn1 = box2;
            
            // 16. Создаем плоскость смещения по Z на R1 от повернутой плоскости
            ksEntity offsetPlaneR1_2 = CreateOffsetPlane(rotatedPlaneFlex, R1);
            
            // 17. Создаем четвертый цилиндр
            ksEntity cylinder4 = CreateCylinder(offsetPlaneR1_2, R2, -D1);
            ksEntity cil2Help_2 = cylinder4;
            
            // 18. Операция пересечения цилиндра с параллелепипедом
            CreateIntersection(cil2Help_2, perehodn1);
            
            return offsetPlaneR1_2
        }

        // Создание плоскости смещения
        private ksEntity CreateOffsetPlane(ksEntity basePlane, double offset)
        {
            ksEntity planeOffset = _part.NewEntity((short)Obj3dType.o3d_planeOffset);
            ksPlaneOffsetParam planeOffsetParam = (ksPlaneOffsetParam)planeOffset.GetDefinition();
            planeOffsetParam.SetPlane(basePlane);
            planeOffsetParam.offset = offset;
            planeOffsetParam.direction = true;
            planeOffset.Create();
            return planeOffset;
        }

        // Создание повернутой плоскости
        private ksEntity CreateRotatedPlane(ksEntity basePlane, double angle, short directionType, string axis = "Y")
        {
            ksEntity planeAngle = _part.NewEntity((short)Obj3dType.o3d_planeAngle);
            ksPlaneAngleParam planeAngleParam = (ksPlaneAngleParam)planeAngle.GetDefinition();
            planeAngleParam.SetPlane(basePlane);
            
            if (axis == "X")
                planeAngleParam.SetAxis(_part.GetDefaultEntity((short)Obj3dType.o3d_axisOX));
            else if (axis == "Y")
                planeAngleParam.SetAxis(_part.GetDefaultEntity((short)Obj3dType.o3d_axisOY));
            else
                planeAngleParam.SetAxis(_part.GetDefaultEntity((short)Obj3dType.o3d_axisOZ));
            
            planeAngleParam.angle = angle * Math.PI / 180;
            planeAngleParam.direction = directionType == (short)Direction_Type.dtNormal;
            planeAngle.Create();
            return planeAngle;
        }

        // Создание цилиндра
        private ksEntity CreateCylinder(ksEntity plane, double radius, double height, double[] origin = null)
        {
            ksEntity cylinder = _part.NewEntity((short)Obj3dType.o3d_cylindric);
            ksCylindricParam cylinderParam = (ksCylindricParam)cylinder.GetDefinition();
            cylinderParam.directionType = (short)Direction_Type.dtNormal;
            cylinderParam.SetPlane(plane);
            
            if (origin != null)
            {
                cylinderParam.SetOrigin(new double[] {origin[0], origin[1], origin[2]});
            }
            
            cylinderParam.radius = radius;
            cylinderParam.height = height;
            cylinder.Create();
            return cylinder;
        }

        // Создание параллелепипеда
        private ksEntity CreateBox(ksEntity plane, double[] corner1, double[] corner2)
        {
            ksEntity box = _part.NewEntity((short)Obj3dType.o3d_baseBox);
            ksBaseBoxParam boxParam = (ksBaseBoxParam)box.GetDefinition();
            boxParam.directionType = (short)Direction_Type.dtNormal;
            boxParam.SetPlane(plane);
            
            // Вычисляем длину, ширину и высоту
            double length = Math.Abs(corner2[0] - corner1[0]);
            double width = Math.Abs(corner2[1] - corner1[1]);
            double height = Math.Abs(corner2[2] - corner1[2]);
            
            boxParam.length = length;
            boxParam.width = width;
            boxParam.height = height;
            boxParam.SetCorner(corner1);
            box.Create();
            return box;
        }

        // Операция пересечения
        private ksEntity CreateIntersection(ksEntity entity1, ksEntity entity2)
        {
            ksEntity intersect = _part.NewEntity((short)Obj3dType.o3d_cut);
            ksCutParam intersectParam = (ksCutParam)intersect.GetDefinition();
            intersectParam.cut = true;
            intersectParam.toolBodies = new ksEntityArray(2);
            intersectParam.toolBodies.Add(entity1);
            intersectParam.toolBodies.Add(entity2);
            intersectParam.directionType = (short)Direction_Type.dtNormal;
            intersect.Create();
            return intersect;
        }

        // Операция вычитания
        private void CreateSubtraction(ksEntity target, ksEntity tool)
        {
            ksEntity subtract = _part.NewEntity((short)Obj3dType.o3d_cut);
            ksCutParam subtractParam = (ksCutParam)subtract.GetDefinition();
            subtractParam.cut = true;
            subtractParam.toolBodies = new ksEntityArray(1);
            subtractParam.toolBodies.Add(tool);
            subtractParam.directionType = (short)Direction_Type.dtNormal;
            subtractParam.SetBaseObject(target);
            subtract.Create();
        }
    }

    public class ParametersLoader
    {
        // Словарь для хранения параметров со значениями по умолчанию
        private Dictionary<string, double> _defaultParams = new Dictionary<string, double>()
        {
            {"L1", 55.0},
            {"L2", 25.0},
            {"L3", 20.0},
            {"R1", 10.0},
            {"R2", 10.0},
            {"R3", 10.0},
            {"R_joint", 15.0},
            {"FlexAngle1", 30.0},
            {"FlexAngle2", 10.0}
        };

        // Словарь для хранения загруженных параметров
        public Dictionary<string, double> Parameters { get; private set; }

        public bool LoadParametersFromFile(string filename)
        {
            // Установка значений по умолчанию
            Parameters = new Dictionary<string, double>(_defaultParams);

            // Проверка существования файла
            if (!File.Exists(filename))
            {
                KompasObject kompas = (KompasObject)System.Runtime.InteropServices.Marshal.GetActiveObject("KOMPAS.Application.5");
                kompas.ksMessageBoxEx("Файл конфигурации " + filename + " не найден. Используются значения по умолчанию.", 
                                     "Предупреждение", 0, 0);
                return false;
            }

            try
            {
                // Чтение файла построчно
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // Пропускаем пустые строки и комментарии
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                        continue;

                    // Разделяем строку на параметр и значение
                    string[] parts = trimmedLine.Split(new[] {'='}, 2);
                    if (parts.Length == 2)
                    {
                        string param = parts[0].Trim();
                        string valueStr = parts[1].Trim();
                        
                        // Пытаемся преобразовать значение в число
                        if (double.TryParse(valueStr, out double value))
                        {
                            if (Parameters.ContainsKey(param))
                                Parameters[param] = value;
                            else
                                Parameters.Add(param, value);
                        }
                        else
                        {
                            KompasObject kompas = (KompasObject)System.Runtime.InteropServices.Marshal.GetActiveObject("KOMPAS.Application.5");
                            kompas.ksMessageBoxEx($"Ошибка в строке: {line}", "Ошибка", 0, 0);
                        }
                    }
                }

                KompasObject kompasSuccess = (KompasObject)System.Runtime.InteropServices.Marshal.GetActiveObject("KOMPAS.Application.5");
                kompasSuccess.ksMessageBoxEx($"Параметры успешно загружены из файла: {filename}", "Информация", 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                KompasObject kompas = (KompasObject)System.Runtime.InteropServices.Marshal.GetActiveObject("KOMPAS.Application.5");
                kompas.ksMessageBoxEx($"Ошибка при чтении файла: {ex.Message}", "Ошибка", 0, 0);
                return false;
            }
        }
    }

    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Введите полный путь к файлу конфигурации:");
            string filename = Console.ReadLine();
            
            ParametersLoader loader = new ParametersLoader();
            loader.LoadParametersFromFile(filename);
            
            FingerModelCreator creator = new FingerModelCreator(loader.Parameters);
            creator.CreateFingerModel();
        }
    }
}