## EasyExpression

![CI](https://github.com/pacheng-csy/EasyExpression-CSharp/actions/workflows/ci.yml/badge.svg)

目标框架
- .netstandard 2.0
- net8.0

### 支持的功能
- 逻辑运算、算术运算、关系运算
- 条件分支 `[IF]` 与多分支匹配 `[CASE]`（惰性求值）
- 函数嵌入与函数嵌套
- 运算符优先级与括号
- 外部传入实参
- 字符串拼接 `[CONCAT]` 及常用字符串函数
- 自定义函数注册
- 可配置的区域性、除零行为与函数白名单

### 基本用法

```
var exp = new Expression("a * (b + c) > d & [CONTAINS](srcText,text)");
var dic = new Dictionary<string, object>
{
    { "a", "3" },
    { "b", "1" },
    { "c", "2" },
    { "d", "4" },
    { "srcText", "abc" },
    { "text", "bc" },
};

// 先绑定实参，再求值
exp.LoadArgument(dic);
var value = exp.Execute();

// 或一步完成绑定和求值
var value2 = exp.Execute(dic);
```

未调用 `LoadArgument` 时直接 `Execute()` 会按空参数绑定一次；表达式中的变量在参与数值运算时会抛出明确的“未提供实参”异常。

### 选项

```
var options = new ExpressionOptions
{
    Culture = CultureInfo.InvariantCulture,              // 数字、日期的解析与格式化
    DivisionByZero = DivisionByZeroBehavior.Throw,       // 除零：Infinity（默认）/ Zero / Throw
    FunctionWhitelist = new HashSet<string> { "SUM", "CONCAT" } // null 或空集合表示不限制
};

var exp = new Expression("a + 1", options);
var value = exp.Execute(new Dictionary<string, object> { { "a", 1.5 } });
```

### 自定义函数

函数名不区分大小写，可指定最少/最多参数个数。同名内置函数默认不允许覆盖。

```
FunctionRegistry.Register("TWICE", values => Convert.ToDouble(values[0]) * 2, 1, 1);
var value = new Expression("[TWICE](3)").Execute(); // 6
FunctionRegistry.Unregister("TWICE");
```

### 内置函数

| 函数 | 说明 | 示例 |
| --- | --- | --- |
| `[SUM](...)` | 求和 | `[SUM](1,2,3)` → 6 |
| `[AVG](...)` | 平均值 | `[AVG](2,4,6)` → 4 |
| `[ROUND](value,digits,mode)` | 精度处理，mode：1 进位 / 0 四舍五入 / -1 舍位 | `[ROUND](3.14,1,1)` → 3.2 |
| `[CONTAINS](source,target)` | 包含 | `[CONTAINS]('abc','b')` → 1 |
| `[EXCLUDING](source,target)` | 不包含 | `[EXCLUDING]('abc','x')` → 1 |
| `[STARTWITH](source,target)` | 开头是 | `[STARTWITH]('abc','a')` → 1 |
| `[ENDWITH](source,target)` | 结尾是 | `[ENDWITH]('abc','c')` → 1 |
| `[EQUALS](a,b)` | 完全匹配 | `[EQUALS]('a','a')` → 1 |
| `[DIFFERENT](a,b)` | 不匹配 | `[DIFFERENT]('a','b')` → 1 |
| `[CONCAT](...)` | 字符串拼接，null 视为空字符串 | `[CONCAT]('a','b')` → ab |
| `[LEN](source)` | 字符串长度 | `[LEN]('abc')` → 3 |
| `[SUBSTR](source,start[,length])` | 截取子串，start 从 1 开始 | `[SUBSTR]('abcdef',2,3)` → bcd |
| `[REPLACE](source,old,new)` | 替换子串 | `[REPLACE]('a-b','-','_')` → a_b |
| `[UPPER](source)` / `[LOWER](source)` | 大小写转换 | `[UPPER]('abc')` → ABC |
| `[TRIM](source)` | 去除首尾空白 | `[TRIM](' a ')` → a |
| `[EDATE](date,delta,unit)` | 日期加减，unit：y/M/d/h/m/s/f | `[EDATE]('2024-01-15',1,M)` |
| `[EODATE](date,months,unit)` | 前后 N 个月的第一天（S）或最后一天（E） | `[EODATE]('2024-01-15',0,E)` |
| `[NOWTIME]()` | 当前时间 | `[NOWTIME]()` |
| `[TIMETOSTRING](date,format)` | 日期格式化 | `[TIMETOSTRING]('2024-05-27',yyyyMMdd)` |
| `[DAYS]` `[HOURS]` `[MINUTES]` `[SECONDS]` `[MILLSECONDS]` | 时间段换算 | `[DAYS]('2024-10-15'-'2024-10-10')` → 5 |
| `[ISNULL](value)` | 是否为空 | `[ISNULL](a)` → 0/1 |
| `[IF](condition,trueValue,falseValue)` | 条件分支，只计算命中的分支 | `[IF](a>0,'正','非正')` |
| `[CASE](value,p1,r1,...,default?)` | 多分支匹配，只计算命中的分支 | `[CASE](2,1,'a',2,'b','c')` → b |

### 字面量与转义

- 数字：`1`、`-2`（一元负号）；百分比文本 `'50%'` 会按 0.5 参与运算
- 字符串使用单引号或双引号：`'abc'`、`"abc"`
- 字符串内 `\\` 表示 `\`，`\'` 表示单引号，`\"` 表示双引号；其余反斜杠原样保留，便于书写 Windows 路径
- `&&` 与 `&`、`||` 与 `|`、`==` 与 `=`、`=>` 与 `>=`、`=<` 与 `<=` 等价，双字符运算符中间允许空格
- 双字符运算符的替换只发生在运算符位置，字符串字面量内的内容不会被改写

### 类型与 null 规则

- 数字、日期、时间段分别按数值、`DateTime`、`TimeSpan` 比较
- 字符串按文本比较；无法转换为数值的文本参与算术或大小比较时抛出 `ExpressionException`
- 参数值为 `null` 时返回 `null`；字符串函数将 `null` 视为空字符串
- 外部传入的字符串会按 `ExpressionOptions.Culture` 尝试转换为数字或日期，函数返回值保留其真实类型

### 异常

表达式为空、语法错误、缺少实参或函数执行失败时会抛出 `ExpressionException`，可通过 `Message` 获取具体原因。解析失败时也可通过 `Expression.ErrorMessage`（兼容旧属性 `ErrorMessgage`）查看。

### 构建与测试

```
dotnet build EasyExpression.sln -c Release
dotnet test EasyExpression.UnitTest/EasyExpression.UnitTest.csproj -c Release
```