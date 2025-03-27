using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

if (!Sse.IsSupported || !Sse3.IsSupported)
{
    Console.WriteLine("SIMD-инструкции не поддерживаются на данной платформе.");
    return;
}
Console.WriteLine("Рендеринг анимации с использованием SIMD-инструкций...");
RendererSimd.RenderAnimation("animation.gif");
Console.WriteLine("Анимация сохранена как animation.gif");

unsafe class RendererSimd
{
    private const int Width = 800;
    private const int Height = 600;

    struct Sphere
    {
        public Vector3 Center;
        public float Radius;
        public Rgba32 Color;
    }

    // Исходные параметры сфер
    private static readonly Sphere[] Spheres =
    [
        new() { Center = new Vector3(-0.5f, 0, 3), Radius = 0.5f, Color = new Rgba32(255, 0, 0) },
        new() { Center = new Vector3(0.5f, 0, 4), Radius = 0.5f, Color = new Rgba32(0, 255, 0) },
        new() { Center = new Vector3(0, 0.5f, 5), Radius = 0.5f, Color = new Rgba32(0, 0, 255) }
    ];

    // Скалярная функция пересечения (для теней)
    static float IntersectSphereScalar(Vector3 rayOrigin, Vector3 rayDir, Sphere sphere)
    {
        Vector3 oc = rayOrigin - sphere.Center;
        float a = Vector3.Dot(rayDir, rayDir);
        float b = 2.0f * Vector3.Dot(oc, rayDir);
        float c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
            return float.PositiveInfinity;
        float t = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
        return t > 0 ? t : float.PositiveInfinity;
    }

    // SIMD‑версия пересечения для группы из 4‑х лучей
    static Vector128<float> IntersectSphereSimd(
        Vector128<float> rayDirX,
        Vector128<float> rayDirY,
        Vector128<float> rayDirZ,
        float ocX,
        float ocY,
        float ocZ,
        float radius)
    {
        Vector128<float> two = Vector128.Create(2.0f);
        Vector128<float> b = Sse.Multiply(two,
            Sse.Add(
                Sse.Add(
                    Sse.Multiply(rayDirX, Vector128.Create(ocX)),
                    Sse.Multiply(rayDirY, Vector128.Create(ocY))
                ),
                Sse.Multiply(rayDirZ, Vector128.Create(ocZ))
            )
        );
        float cScalar = ocX * ocX + ocY * ocY + ocZ * ocZ - radius * radius;
        Vector128<float> c = Vector128.Create(cScalar);
        Vector128<float> b2 = Sse.Multiply(b, b);
        Vector128<float> four = Vector128.Create(4.0f);
        Vector128<float> disc = Sse.Subtract(b2, Sse.Multiply(four, c));
        Vector128<float> mask = Sse.CompareGreaterThan(disc, Vector128<float>.Zero);
        Vector128<float> sqrtDisc = Sse.Sqrt(disc);
        Vector128<float> negB = Sse.Subtract(Vector128<float>.Zero, b);
        Vector128<float> numerator = Sse.Subtract(negB, sqrtDisc);
        Vector128<float> t = Sse.Divide(numerator, Vector128.Create(2.0f));
        Vector128<float> posInf = Vector128.Create(float.PositiveInfinity);
        t = Sse.Or(Sse.And(mask, t), Sse.AndNot(mask, posInf));
        return t;
    }

    // Функция рендеринга анимации (GIF)
    public static void RenderAnimation(string filename)
    {
        int numFrames = 60; // число кадров для полного оборота

        // Список для хранения кадров (ImageFrame<Rgba32>) анимации
        List<ImageFrame<Rgba32>> frames = new List<ImageFrame<Rgba32>>();

        // Для каждого кадра генерируем повёрнутые позиции сфер и рендерим сцену
        for (int f = 0; f < numFrames; f++)
        {
            float angle = f * (2 * MathF.PI / numFrames);

            // Центр вращения (пивот) – вычисленный или заданный вручную
            Vector3 pivot = new Vector3(0, 0.1666f, 4);

            // Поворачиваем каждую сферу вокруг центра pivot
            Sphere[] rotatedSpheres = new Sphere[Spheres.Length];
            for (int s = 0; s < Spheres.Length; s++)
            {
                Sphere orig = Spheres[s];
                Vector3 relative = orig.Center - pivot;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                float newX = cos * relative.X + sin * relative.Z;
                float newZ = -sin * relative.X + cos * relative.Z;
                Vector3 newCenter = new Vector3(newX, relative.Y, newZ) + pivot;
                rotatedSpheres[s] = orig with { Center = newCenter };
            }

            // Создаём изображение для текущего кадра
            using Image<Rgba32> frameImage = new Image<Rgba32>(Width, Height);
            frameImage.Mutate(ctx => ctx.Clear(new Rgba32(0, 0, 0)));
            Vector3 cameraOrigin = new Vector3(0, 0, -1);
            Vector3 lightPos = new Vector3(0, 5, 5);
            float shadowEpsilon = 0.001f;

            for (int j = 0; j < Height; j++)
            {
                float v = (j - Height / 2f) / (Height / 2f);
                for (int i = 0; i < Width; i += 4)
                {
                    // Вычисляем горизонтальные координаты для группы из 4-х пикселей
                    float[] uValues = new float[4];
                    for (int k = 0; k < 4; k++)
                    {
                        int x = i + k;
                        uValues[k] = (x - Width / 2f) / (Width / 2f);
                    }
                    fixed (float* uPtr = uValues)
                    {
                        Vector128<float> rayDirX = Sse.LoadVector128(uPtr);
                        Vector128<float> rayDirY = Vector128.Create(-v);
                        Vector128<float> rayDirZ = Vector128.Create(1.0f);

                        // Нормализуем направление лучей
                        Vector128<float> x2 = Sse.Multiply(rayDirX, rayDirX);
                        Vector128<float> y2 = Sse.Multiply(rayDirY, rayDirY);
                        Vector128<float> z2 = Sse.Multiply(rayDirZ, rayDirZ);
                        Vector128<float> sum = Sse.Add(Sse.Add(x2, y2), z2);
                        Vector128<float> invLen = Sse.Divide(Vector128.Create(1.0f), Sse.Sqrt(sum));
                        rayDirX = Sse.Multiply(rayDirX, invLen);
                        rayDirY = Sse.Multiply(rayDirY, invLen);
                        rayDirZ = Sse.Multiply(rayDirZ, invLen);

                        float[] tBest =
                        [
                            float.PositiveInfinity,
                            float.PositiveInfinity,
                            float.PositiveInfinity,
                            float.PositiveInfinity
                        ];
                        int[] sphereIndex = [-1, -1, -1, -1];

                        // Проверяем пересечение лучей со всеми сферами
                        for (int s = 0; s < rotatedSpheres.Length; s++)
                        {
                            Sphere sphere = rotatedSpheres[s];
                            float ocX = cameraOrigin.X - sphere.Center.X;
                            float ocY = cameraOrigin.Y - sphere.Center.Y;
                            float ocZ = cameraOrigin.Z - sphere.Center.Z;
                            Vector128<float> tVals = IntersectSphereSimd(rayDirX, rayDirY, rayDirZ, ocX, ocY, ocZ, sphere.Radius);
                            float[] tArray = new float[4];
                            fixed (float* tPtr = tArray)
                            {
                                Sse.Store(tPtr, tVals);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                if (tArray[k] < tBest[k])
                                {
                                    tBest[k] = tArray[k];
                                    sphereIndex[k] = s;
                                }
                            }
                        }

                        // Извлекаем компоненты лучей для теневого теста
                        float[] rayDirXArr = new float[4];
                        float[] rayDirYArr = new float[4];
                        float[] rayDirZArr = new float[4];
                        fixed (float* ptrX = rayDirXArr)
                        fixed (float* ptrY = rayDirYArr)
                        fixed (float* ptrZ = rayDirZArr)
                        {
                            Sse.Store(ptrX, rayDirX);
                            Sse.Store(ptrY, rayDirY);
                            Sse.Store(ptrZ, rayDirZ);
                        }

                        // Для каждого из 4-х пикселей проводим shadow‑тест и задаём цвет
                        for (int k = 0; k < 4; k++)
                        {
                            int x = i + k;
                            Rgba32 pixelColor = new Rgba32(0, 0, 0);
                            if (sphereIndex[k] >= 0)
                            {
                                Sphere hitSphere = rotatedSpheres[sphereIndex[k]];
                                Vector3 rayDir = new Vector3(rayDirXArr[k], rayDirYArr[k], rayDirZArr[k]);
                                float tHit = tBest[k];
                                Vector3 p = cameraOrigin + rayDir * tHit;
                                Vector3 n = Vector3.Normalize(p - hitSphere.Center);
                                Vector3 pOffset = p + shadowEpsilon * n;
                                Vector3 toLight = lightPos - pOffset;
                                float lightDistance = toLight.Length();
                                Vector3 l = Vector3.Normalize(toLight);
                                bool inShadow = false;
                                for (int s = 0; s < rotatedSpheres.Length; s++)
                                {
                                    float tShadow = IntersectSphereScalar(pOffset, l, rotatedSpheres[s]);
                                    if (tShadow < lightDistance)
                                    {
                                        inShadow = true;
                                        break;
                                    }
                                }
                                if (inShadow)
                                {
                                    pixelColor = new Rgba32(
                                        (byte)(hitSphere.Color.R * 0.5f),
                                        (byte)(hitSphere.Color.G * 0.5f),
                                        (byte)(hitSphere.Color.B * 0.5f));
                                }
                                else
                                {
                                    pixelColor = hitSphere.Color;
                                }
                            }
                            frameImage[x, j] = pixelColor;
                        }
                    }
                }
            }
            // Клонируем кадр, используя ClonePixelData() и создаём новый кадр через Image.LoadPixelData(...)
            // Image<Rgba32> clonedFrame = frameImage.CloneAs<Rgba32>();
            // // Добавляем корневой кадр в список
            // frames.Add(clonedFrame.Frames[0]);
            
            using (var ms = new MemoryStream())
            {
                // Сохраняем текущий кадр во временный поток с использованием PNG-кодировщика
                frameImage.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                ms.Position = 0;
                // Загружаем кадр из потока, создавая глубокую копию
                var clonedImage = Image.Load<Rgba32>(ms);
                frames.Add(clonedImage.Frames.RootFrame);
            }
            Console.WriteLine($"Кадр {f + 1}/{numFrames} отрисован.");
        }
        
        // Создаём итоговую GIF-анимацию, используя первый кадр как основу
        using var gif = new Image<Rgba32>(Width, Height, Rgba32.ParseHex("#000000"));
        
        for (int i = 1; i < frames.Count; i++)
        {
            gif.Frames.AddFrame(frames[i]);
        }
        gif.Frames.RemoveFrame(0);
        // Устанавливаем бесконечное повторение GIF
        gif.Metadata.GetGifMetadata().RepeatCount = 0;
        gif.Save(filename);
    }
}
