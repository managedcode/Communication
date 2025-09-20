# Claude Code Agents for ManagedCode.Communication

This project includes specialized Claude Code agents for comprehensive code review and quality assurance.

## Available Agents

### 1. 🔍 Result Classes Reviewer (`result-classes-reviewer`)

**Specialization**: Expert analysis of Result pattern implementation
**Focus Areas**:
- Interface consistency between Result, Result<T>, CollectionResult<T>
- JSON serialization attributes and patterns
- Performance optimization opportunities
- API design consistency

**Usage**: Invoke when making changes to core Result classes or interfaces

### 2. 🏗️ Architecture Reviewer (`architecture-reviewer`)

**Specialization**: High-level project structure and design patterns
**Focus Areas**:
- Project organization and dependency management
- Design pattern implementation quality
- Framework integration architecture
- Scalability and maintainability assessment

**Usage**: Use for architectural decisions and major structural changes

### 3. 🛡️ Security & Performance Auditor (`security-performance-auditor`)

**Specialization**: Security vulnerabilities and performance bottlenecks
**Focus Areas**:
- Input validation and information disclosure risks
- Memory allocation patterns and async best practices
- Resource management and potential performance issues
- Security antipatterns and vulnerabilities

**Usage**: Run before production releases and during security reviews

### 4. 🧪 Test Quality Analyst (`test-quality-analyst`)

**Specialization**: Test coverage and quality assessment
**Focus Areas**:
- Test coverage gaps and edge cases
- Test design quality and maintainability
- Integration test completeness
- Testing strategy recommendations

**Usage**: Invoke when updating test suites or evaluating test quality

### 5. 🎯 API Design Reviewer (`api-design-reviewer`)

**Specialization**: Public API usability and developer experience
**Focus Areas**:
- API consistency and naming conventions
- Developer experience and discoverability
- Documentation quality and examples
- Framework integration patterns

**Usage**: Use when designing new APIs or refactoring public interfaces

## How to Use the Agents

### Option 1: Via Task Tool (Recommended)
```
Task tool with subagent_type parameter - currently requires general-purpose agent as proxy
```

### Option 2: Direct Invocation (Future)
```
Once Claude Code recognizes the agents, they can be invoked directly
```

## Agent File Locations

All agents are stored in:
```
.claude/agents/
├── result-classes-reviewer.md
├── architecture-reviewer.md
├── security-performance-auditor.md
├── test-quality-analyst.md
└── api-design-reviewer.md
```

## Comprehensive Review Process

For a complete project audit, run agents in this order:

1. **Architecture Reviewer** - Get overall structural assessment
2. **Result Classes Reviewer** - Focus on core library consistency
3. **Security & Performance Auditor** - Identify security and performance issues
4. **Test Quality Analyst** - Evaluate test coverage and quality
5. **API Design Reviewer** - Review public API design and usability

## Recent Audit Findings Summary

### ✅ Major Strengths Identified
- Excellent Result pattern implementation with proper type safety
- Outstanding framework integration (ASP.NET Core, Orleans)
- Strong performance characteristics using structs
- RFC 7807 compliance and proper JSON serialization
- Comprehensive railway-oriented programming support

### ⚠️ Areas for Improvement
- Minor JSON property ordering inconsistencies
- Some LINQ allocation hotspots in extension methods
- Missing ConfigureAwait(false) in async operations
- Information disclosure risks in exception handling
- Test coverage gaps in edge cases

### 🚨 Critical Issues Addressed
- Standardized interface hierarchy and removed redundant interfaces
- Fixed missing JsonIgnore attributes
- Improved logging infrastructure to avoid performance issues
- Added proper IsValid properties across all Result types

## Contributing to Agent Development

When creating new agents:

1. Follow the established YAML frontmatter format
2. Include specific tools requirements
3. Provide clear focus areas and review processes
4. Include specific examples and code patterns to look for
5. Define clear deliverable formats

## Continuous Improvement

These agents should be updated as the project evolves:
- Add new review criteria as patterns emerge
- Update security checklist based on new threats
- Enhance performance patterns as bottlenecks are identified
- Expand API design guidelines based on user feedback

The agents represent institutional knowledge and should be maintained alongside the codebase.