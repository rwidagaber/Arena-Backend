# Enhance Equipment Category Selection in MVC

Currently, adding or editing gym equipment uses a simple text input for "Category". We want to retrieve existing categories from the database, allow the administrator to select multiple categories using a multi-select control, and allow adding new categories dynamically.

## User Review Required

> [!NOTE]
> - Since categories are stored as a single string field (`Category`) in the `Equipment` database model, we will join selected categories using a comma (e.g. `"Free Weights, Cardio"`) and save them. When editing or retrieving categories, we split by commas to display individual badges/options.
> - The dynamic "Add New Category" feature will allow the admin to add custom categories to the list on-the-fly. The new category will be added to the selection list, auto-checked, and stored in the database when the form is submitted.

## Proposed Changes

---

### Component: Application (Services)

#### [MODIFY] [IEquipmentService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/IServices/IEquipmentService.cs)
- Add a new interface method `Task<Result<List<string>>> GetCategoriesAsync();` to retrieve all distinct categories in the database.

#### [MODIFY] [EquipmentService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Services/Gym/EquipmentService.cs)
- Implement `GetCategoriesAsync()` which:
  - Fetches all distinct `Category` entries.
  - Splits entries by comma to separate multi-category assignments.
  - Returns a unique, trimmed, sorted list of categories.

---

### Component: Infrastructure (Localization)

#### [MODIFY] [en-US.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/en-US.json)
- Add English keys:
  - `"SelectCategories": "Select Categories"`
  - `"AddNewCategory": "Add New Category"`
  - `"EnterNewCategory": "Enter new category name..."`
  - `"Add": "Add"`

#### [MODIFY] [ar-EG.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/ar-EG.json)
- Add Arabic keys:
  - `"SelectCategories": "اختر الفئات"`
  - `"AddNewCategory": "إضافة فئة جديدة"`
  - `"EnterNewCategory": "أدخل اسم الفئة الجديدة..."`
  - `"Add": "إضافة"`

---

### Component: MVC Web Application (Presentation)

#### [MODIFY] [EquipmentsController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/EquipmentsController.cs)
- Modify the `Create` (GET & POST) actions to populate `ViewBag.Categories` using the new `IEquipmentService.GetCategoriesAsync()` method.
- Modify the `Edit` (GET & POST) actions to similarly populate `ViewBag.Categories`.

#### [MODIFY] [Create.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Equipments/Create.cshtml)
- Replace the text input `<input asp-for="Category" ... />` with a custom dropdown-style multi-select component.
- The multi-select component will list existing categories as checkboxes.
- Include a text input and button for "Add New Category" inside the control.
- When clicked, JavaScript will append the new category to the checkbox list, check it, and focus.
- Add JavaScript to compile all selected checkboxes into a comma-separated list and set the value of a hidden `<input asp-for="Category" />` field before submission.
- Style the UI nicely using CSS variables and theme colors (e.g. `var(--yellow)` and `var(--card-bg)`) to match the existing premium design.

#### [MODIFY] [Edit.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Equipments/Edit.cshtml)
- Apply the same multi-select and dynamic category addition UI.
- Pre-populate checkbox states by checking which existing categories (split by comma from `Model.Category`) match the list.

## Verification Plan

### Automated Tests
- Run `dotnet build` to ensure the C# code changes compile cleanly.

### Manual Verification
- Navigate to the `/Equipments/Create` page.
- Verify that a list of existing categories is displayed as multi-select options.
- Click the "Add New Category" button, enter a custom category name, and verify it gets added and checked.
- Select multiple categories and submit the form. Verify the equipment is created with the chosen categories in the list view.
- Navigate to the `/Equipments/Edit` page for the created equipment. Verify that the correct categories are pre-selected and that updating them works as expected.
