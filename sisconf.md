# CustomUninstaller

## Структура проекта

```
CustomUninstaller/
├── CustomUninstaller.csproj
├── app.manifest
├── Program.cs
├── Core/
│   ├── Models.cs
│   ├── Logger.cs
│   ├── Filter.cs
│   └── Manager.cs
└── UI/
    └── MainForm.cs
```

## Описание файлов

- **CustomUninstaller.csproj** — файл проекта .NET
- **app.manifest** — манифест приложения (настройки совместимости, права доступа)
- **Program.cs** — точка входа в приложение
- **Core/** — основные логические компоненты
  - **Models.cs** — модели данных
  - **Logger.cs** — система логирования
  - **Filter.cs** — фильтрация элементов
  - **Manager.cs** — управление процессами удаления
- **UI/** — пользовательский интерфейс
  - **MainForm.cs** — главная форма приложения
