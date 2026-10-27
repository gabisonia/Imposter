# Release notes

## Unreleased

### API changes

- Removed legacy binary serialization support from `MissingImposterException`: the
  `[Serializable]` attribute, protected `(SerializationInfo, StreamingContext)`
  constructor, and `GetObjectData` override. This minor API change removes an obsolete
  call flagged by CodeQL (#424). Consumers that derive from the exception and call
  its serialization constructor must use a public constructor instead. Binary
  serialization of this exception is no longer supported. The three public
  constructors, `MethodName` property, messages, and inner exception behavior are
  unchanged.
