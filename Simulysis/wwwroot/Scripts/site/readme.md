# Cấu trúc SVP scripts

## loader.js

Tạo loading screen

## noop.js

Tập hợp các hàm đơn giản được sử dụng làm hàm mặc định khi truyền hàm nhằm tránh lỗi dạng `x is not a function`

```js
function foo(callback) {
  callback()
}

foo() // Error: callback is not a function

function bar(callback = noop) {...}
bar() // no error
```

## pagination.js

Tạo phân trang cho các bảng biểu

## treeview.js

Điều khiên đóng mở các tab của sidebar

## Thư mục

Được phân chia thành các file dưới dạng module

- `api`: gồm các module chứa các hàm gọi API từ backend để query nội dung file, quan hệ hoặc search
- `draw`: gồm một module _draw_ và hai module con là hai bản vẽ chính của project _model draw_ và _network draw_. Mỗi một bản vẽ được để dưới dạng module sẽ cho phép reuse ở nhiều trang khác nhau.

```js
var modelDraw = Object.create(ModelDraw)
modelDraw.init(options)

var networkDraw = Object.create(NetworkDraw)
networkDraw.init(options)
```

- `pages`: gồm các module chịu trách nhiệm cho từng trang
  - _show-file_: trang khi người dùng mở một file
  - _network-extend_: trang khi người dùng pop-up network view
  - _trackline_: trang khi người dùng pop-up model view
- `utils`: gồm các module chứa các hàm hỗ trợ

# Pattern sử dụng

## Class trong JavaScript

`class` trong JavaScript không giống với `class` trong các ngôn ngữ OOP như Java hay C++. Với các ngôn ngữ này, khi tạo một `new SomeClass()`, các properties và methods của class đều được copy sang instance mới được tạo. Riêng với JavaScript, nó sẽ tạo ra một `prototypal link`.

```js
class Person {
	constructor(name) {
		this.name = name
	}

	speak() {
		console.log('Hello')
	}
}

var p1 = new Person('A')
```

Thực chất những gì đoạn code bên trên thực hiện là đoạn code dưới đây

```js
function Person(name) {
	this.name = name
}
Person.prototype.speak = function () {
	console.log('Hello')
}

var p1 = new Person('A')
```

Mỗi một hàm trong JS đều là một tổ hợp function-object. Với ví dụ trên, khi dùng `Person()` thì là gọi phần function, khi dùng dot notation `Person.` thì là truy cập vào phần object. Phần object luôn có một property tên là `prototype` và bản thân `prototype` là một object. Ở ví dụ này, lệnh `var p1 = new Person('A')` tạo ra một link nối object `p1` với object `Person.prototype` của hàm `Person`. Do đó các properties và methods sẽ không được copy sang `p1` mà được link đến `p1`.

```js
Person.prototype // { constructor: {...}, speak: {...} }

p1 // { name: 'A', __proto__: { constructor: {...}, speak: {...} } }

p1.__proto__ === Person.prototype // true
```

## OLOO (Objects Linked To Other Objects)

`class` là một syntax dễ gây hiểu lầm. Để tránh việc sử dụng `class`, project dùng một cách tiếp cận JavaScript hơn, OLOO pattern, liên kết giữa các object thuần. Đoạn code phía trên có thể được viết lại như sau

```js
var Person = {
	init(name) {
		this.name = name
	},
	speak() {
		console.log('Hello')
	}
}

var p1 = Object.create(Person) // Object.create tạo một prototypal link đến object được truyền vào, ở đây là Person
p1.init('A')

p1.__proto__ === Person // true
```

Các object trong JS đều có sẵn một prototypal link đến `Object.prototype`, và `Object.prototype` sẽ link đến `null`. Do đó, trong ví dụ trên, prototypal chain là `p1` &rarr; `Person` &rarr; `Object.prototype` &rarr; `null`. Khi tìm một property trong một object, JS sẽ tìm lần lượt trong object đó, rồi đi ngược lên theo prototypal chain đến khi tìm được hoặc chạm `null`. Điều này lí giải tại sao `p1` không có method `speak` nhưng vẫn có thể gọi được. Prototypal chain cũng chính là cơ chế JS sử dụng để làm kế thừa trong `class`.

```js
class A {
  constructor() {...}
  foo() {...}
}

class B extends A {
  constructor() {
    super()
    ...
  }
  bar() {...}
}

var x = new B() // x -> B.prototype -> A.prototype -> Object.prototype -> null
```

Viết lại theo OLOO

```js
var A = {
  init() {...}
  foo() {...}
}

var B = Object.create(A)
B.initB = function() {
  this.init()
  ...
}
B.bar = function() {...} // Hoặc sử dụng Object.assign

var x = Object.create(B) // x -> B -> A -> Object.prototype -> null
```

Điểm trừ của OLOO là không thể sử dụng tên các method giống nhau, còn gọi là `property shadowing` hoặc override. Để có thể sử dụng được `super` và override method, ta viết lại như sau

```js
var A = {
  init() {...}
  foo() {...}
}

var B = {
  __proto__: A,
  // Property shadowing
  init() {
    super.init()
    ...
  }
  bar() {...}
}

var x = Object.create(B) // x -> B -> A -> Object.prototype -> null
```

Do JS link các object với nhau dẫn tới việc nếu một property là array hoặc object thì có thể sinh ra bug.

```js
var A = {
  arr: [],
  add() {
    this.arr.push(1)
  }
}

var B = {
  __proto__: A
  add2() {
    this.arr.push(2)
  }
}

A.add()

B.arr // [1]
A.arr // [1]

B.add2()

B.arr // [1, 2]
A.arr // [1, 2]

B.arr === A.arr // true
```
Nếu không assign lại property `arr` trong `B` thì `B.arr` cũng chính là `A.arr` và sinh ra bug. Để hiểu thêm về `property shadowing`, có thể search về `property shadowing` và `hasOwnProperty`

## `this` và `super` trong JavaScipt

### `this`

Về cơ bản, `this` được xác định một cách dynamic, tức là phụ thuộc vào cái gì đứng trước dấu chấm khi gọi hàm.

```js
var Foo = {
	value: 10,
	bar() {
		console.log(this.value)
	}
}

Foo.bar() // this trong bar() là Foo
document.addEventListener('click', Foo.bar) // this là undefined -> lỗi
document.addEventListener('click', Foo.bar.bind(Foo)) // sau khi được bind thì this lại là Foo
```

Ngoài ra, các `function` sẽ có `this` riêng, còn `arrow function` sẽ không có `this`. Có thể search thêm về `this` trong JavaScript để hiểu rõ hơn

### `super`

Khác với `this`, `super` là static, được gắn liền khi định nghĩa object. Quay lại với ví dụ kế thừa `A` và `B` ở trên. Nếu viết lại theo cách sử dụng `Object.assign`

```js
var A = { init() {...}, foo() {...} }

var B = Object.assign(Object.create(A), {
  init() {
    super.init() // Lỗi do super gắn với object literal khi định nghĩa, và object này không có hàm init
    ...
  },
  bar() {...}
})
```
