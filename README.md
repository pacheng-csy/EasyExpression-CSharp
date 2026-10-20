## EasyExpression

依赖
- .netstandard 2.0

### 支持的功能
- 逻辑运算
- 算术运算
- 条件运算
- 函数嵌入
- 优先级解构
- 外部传入实参
- 字符串拼接（`[CONCAT]`）
- 自定义函数注册
- 可配置的区域性（数字、日期的解析与格式化）

### 函数扩展

内置函数定义在 `EasyExpression.FormulaAction` 中，通过 `FunctionRemarkAttribute` 标注函数名。

也可以在运行时注册自定义函数（函数名不区分大小写）：

```
FunctionRegistry.Register("TWICE", values => Convert.ToDouble(values[0]) * 2, 1, 1);
var value = new Expression("[TWICE](3)").Execute(); // 6
FunctionRegistry.Unregister("TWICE");
```

### 逻辑运算
**0d** 表示 **false**
**1d** 表示 **true**

### 示例
- 逻辑表达式

```
 var expStr = "3 * (1 + 2) <= 5 || !(8 / (4 - 2) > [SUM](1,2,3))";
 // 解析表达式树
 var exp = new Expression(expStr);
 // 把表达式中的数据加载到表达式对应的节点
 exp.LoadArgument();
 // 按表达式树逐级执行结果
 var value = exp.Execute();
 ```

  ```
    var expStr = "a * (b + c) > d & [CONTAINS](srcText,text)";
    var dic = new Dictionary<string, object>
    {
        { "a","3"},
        { "b","1"},
        { "c","2"},
        { "d","4"},
        { "srcText","abc"},
        { "text","bc"},
    };
    var exp = new Expression(expStr);
    exp.LoadArgument(dic);
    var value = exp.Execute();
 ```

```
    var expStr = "!((![ISNULL](a)) && a != '' && (([ISNULL](b)) || b == '') || ((![ISNULL](b)) && b != '' && (([ISNULL](a)) || a == '')))";
    var exp = new Expression(expStr);
    var dic = new Dictionary<string, object>()
    {
	    {"a",null},
	    {"b","abc"},
    };
    exp.LoadArgument(dic);
    var value = exp.Execute();
 ```

- 算术表达式

 ```
 var expStr = "3 * (1 + 2) + 5 - (30 / (4 - 2) % [SUM](1,2,3))";
 var exp = new Expression(expStr);
 exp.LoadArgument();
 var value = exp.Execute();
```
- 日期计算及处理

```
var expStr = "[ROUND]([DAYS]('2024-10-15'-'2024-10-10') / 30,1,0)";
var exp = new Expression(expStr);
exp.LoadArgument();
var value = exp.Execute();
```

- 字符串拼接

```
var expStr = "[CONCAT]('Hello, ', name)";
var exp = new Expression(expStr);
exp.LoadArgument(new Dictionary<string, object> { { "name", "EasyExpression" } });
var value = exp.Execute(); // "Hello, EasyExpression"
```

- 外部传入实参

```
 var expStr = "a * (b + c) + 5 - (30 / (d - 2) % [SUM](1,2,3))";
 var dic = new Dictionary<string,object>
 {
    { "a","3"},
    { "b","1"},
    { "c","2"},
    { "d","4"},
 };
 var exp = new Expression(expStr);
 // 外部实参注入到表达式树中
 exp.LoadArgument(dic);
 var value = exp.Execute();

 // 也可以一步完成绑定和计算
 var value2 = exp.Execute(dic);
```

- 指定区域性与一次完成计算

```
var options = new ExpressionOptions { Culture = CultureInfo.InvariantCulture };
var exp = new Expression("a + 1", options);
var value = exp.Execute(new Dictionary<string, object> { { "a", 1.5 } }); // 2.5
```

### 异常

表达式为空、语法错误、缺少实参或函数执行失败时会抛出 `ExpressionException`，可通过 `Message` 获取具体原因。