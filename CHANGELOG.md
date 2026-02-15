# Changelog

All notable changes to this project will be automatically documented in this file.


## [1.6.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.5.0...v1.6.0) (2026-02-15)


### Features

* Add repeating section support for collection types ([#36](https://github.com/DrBarnabus/CompositeKey/issues/36)) ([7675481](https://github.com/DrBarnabus/CompositeKey/commit/7675481fa523de355192df4062a4b23df94cb5f9))

## [1.5.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.4.0...v1.5.0) (2025-11-16)


### Features

* Add partial key formatting methods ([#33](https://github.com/DrBarnabus/CompositeKey/issues/33)) ([42f9674](https://github.com/DrBarnabus/CompositeKey/commit/42f9674ffe55ce447e636819dae2e15fea5d1286))
* Add analyzer for real-time IDE feedback ([#29](https://github.com/DrBarnabus/CompositeKey/issues/29)) ([8cddd06](https://github.com/DrBarnabus/CompositeKey/commit/8cddd06babab3c0623ef318d7ae1e4635e6ecf6e))
* Expand analyzer rules to cover template strings ([#30](https://github.com/DrBarnabus/CompositeKey/issues/30)) ([3c94496](https://github.com/DrBarnabus/CompositeKey/commit/3c94496b7b75ebbdcc0581d3cd58071615b19bb7))
* Expand analyzer rules to cover property constraints ([#31](https://github.com/DrBarnabus/CompositeKey/issues/31)) ([3f5a44c](https://github.com/DrBarnabus/CompositeKey/commit/3f5a44c946739a94f4d6d26cf6b9b63601789347))

## [1.4.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.3.1...v1.4.0) (2025-07-04)


### Features

* Allow keys to reference the same property more than once ([#25](https://github.com/DrBarnabus/CompositeKey/issues/25)) ([0d696bf](https://github.com/DrBarnabus/CompositeKey/commit/0d696bf0eab23e63449b9136200d5bdae536c5d1))

### [1.3.1](https://github.com/DrBarnabus/CompositeKey/compare/v1.3.0...v1.3.1) (2025-05-23)

_Updated dependencies_

## [1.3.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.2.0...v1.3.0) (2024-10-29)


### Features

* Fast path `Format` generation for string properties ([#15](https://github.com/DrBarnabus/CompositeKey/issues/15)) ([b3a59e4](https://github.com/DrBarnabus/CompositeKey/commit/b3a59e44a1c76f20fd71aeec2dae5196e802e59d))
* Fast path `Format`/`Parse` generation for Enums ([#10](https://github.com/DrBarnabus/CompositeKey/issues/10)) ([858ba21](https://github.com/DrBarnabus/CompositeKey/commit/858ba212a359434c09a1abc9229209fac4ad7ffb))


### Bug Fixes

* Source Generator now correctly targets all .NET 8 SDK Versions ([#13](https://github.com/DrBarnabus/CompositeKey/issues/13)) ([93c912f](https://github.com/DrBarnabus/CompositeKey/commit/93c912fffa825e1818c6c98e8633991cca35f185))

## [1.2.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.1.0...v1.2.0) (2024-10-10)


### Features

* Add option to disable/enable `InvariantCulture` ([#9](https://github.com/DrBarnabus/CompositeKey/issues/9)) ([b61a4a4](https://github.com/DrBarnabus/CompositeKey/commit/b61a4a4ba7facd11c4ff17e8e72baecdb61eada0))


### Bug Fixes

* Fix typo in UnsupportedType and NoObviousConstructor diagnostics ([#7](https://github.com/DrBarnabus/CompositeKey/issues/7)) ([3f1f9a0](https://github.com/DrBarnabus/CompositeKey/commit/3f1f9a02b9a7c89d2b08deee8e47c36af1eba6d2))

## [1.1.0](https://github.com/DrBarnabus/CompositeKey/compare/v1.0.1...v1.1.0) (2024-09-24)


### Features

* Allow `[CompositeKey]` to be on a nested private record ([30c225a](https://github.com/DrBarnabus/CompositeKey/commit/30c225a3fa406f70cfd4bb757e71f55392eaf809))
* Enable marking explicit constructor with `[CompositeKeyConstructor]` attribute ([739b60c](https://github.com/DrBarnabus/CompositeKey/commit/739b60c14366b689a96ff095d87d8336ad200ddc))
* Optimization of generated `ToString`/`To...Key` methods when using Constant or Guid key parts ([556a2ba](https://github.com/DrBarnabus/CompositeKey/commit/556a2ba308d538ca7fbdde2ec259060cb4ba77b2))

### [1.0.1](https://github.com/DrBarnabus/CompositeKey/compare/v1.0.0...v1.0.1) (2024-08-30)


### Bug Fixes

* Analyzer not being packaged correctly ([9a84841](https://github.com/DrBarnabus/CompositeKey/commit/9a8484138320f6422ab98e4cf86819b1d7c6d706))

## 1.0.0 (2024-08-21)


### Features

* Initial Implementation ([ecbfd8e](https://github.com/DrBarnabus/CompositeKey/commit/ecbfd8e38b76aec713a253861ffc6270d089f6e4))
* Report error diagnostics when parser fails ([6aa31a1](https://github.com/DrBarnabus/CompositeKey/commit/6aa31a16d306a9d7a39e888b3cf9526bd097b16e))


### Bug Fixes

* Add missing `using System;` statement to generated code ([332ccaf](https://github.com/DrBarnabus/CompositeKey/commit/332ccaf1bb75370b6cfee0ac5cac2011de6dc38f))
* Allow CompositeId type to be `sealed` ([138986a](https://github.com/DrBarnabus/CompositeKey/commit/138986a10dff5678f9995996c8032ac065cade5d))
* Allow singular value as PartitionKey or SortKey ([5045d27](https://github.com/DrBarnabus/CompositeKey/commit/5045d279b53dfe45db5a2cc7a7cba5d7897269f6))
* Incorrect `TSelf` when generating for nested type declaration ([2d2a7c6](https://github.com/DrBarnabus/CompositeKey/commit/2d2a7c60afc6877e48efd94838d9a41de306c595))
* Incorrect type keyword usage in TargetTypeDeclarations ([2735904](https://github.com/DrBarnabus/CompositeKey/commit/273590410a51d6a4120290201c9eab1543e878ae))
