namespace GZCTF.Features.SkillTrees.Application;

public sealed class SkillTreeNotFoundException : Exception;
public sealed class SkillTreeRevisionConflictException : Exception;
public sealed class SkillTreeValidationException(string message) : Exception(message);
public sealed class SkillTreeConfirmationMismatchException : Exception;
public sealed class SkillTreeInvalidIconException : Exception;

public sealed class SkillCategoryNotFoundException : Exception;
public sealed class SkillCategoryValidationException(string message) : Exception(message);
public sealed class SkillCategoryMergeConflictException(string message) : Exception(message);
public sealed class SkillCategoryConfirmationMismatchException : Exception;

public sealed class ContentCategoryRequiredException : Exception;
public sealed class ContentCategoryHasNoTreeException : Exception;
public sealed class ContentPublicationValidationException(string message) : Exception(message);
public sealed class SkillTreeEnrollmentNotFoundException : Exception;
