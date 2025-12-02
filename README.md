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

### 函数扩展

在EasyExpression.FormulaAction下可以实现更多函数

### 逻辑运算
**0d** 表示 **false**
**1d** 表示 **true**

### 示例
- 逻辑表达式

```
 var expStr = "3 * (1 + 2) < != 5 || !(8 / (4 - 2) > [SUM](1,2,3))";
 // 解析表达式树
 var exp = new Expression(expStr);
 // 把表达式中的数据加载到表达式对应的节点
 exp.LoadArgument();
 // 按表达式树逐级执行结果
 var value = exp.Excute();
 ```

  ```
    var expStr = "a * (b + c) > d & [Contains](srcText,text)";
    var dic = new Dictionary<string, string>
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
    var value = exp.Excute();
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
 var value = exp.Excute();
```
- 日期计算及处理

```
var expStr = "[ROUND]([DAYS]('2024-10-15'-'2024-10-10') / 30,1,0)";
var exp = new Expression(expStr);
exp.LoadArgument();
var value = exp.Execute();
            
```

- 外部传入实参

```
 var expStr = "a * (b + c) + 5 - (30 / (d - 2) % [SUM](1,2,3))";
 var dic = new Dictionary<string,string>
 {
    { "a","3"},
    { "b","1"},
    { "c","2"},
    { "d","4"},
 };
 var exp = new Expression(expStr);
 // 外部实参注入到表达式树中
 exp.LoadArgument(dic);
 var value = exp.Excute();
            
```
