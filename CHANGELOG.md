# Changelog

All notable changes to this project will be automatically documented in this file.


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
