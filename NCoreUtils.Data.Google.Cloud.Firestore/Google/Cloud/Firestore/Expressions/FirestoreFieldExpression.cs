using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

public abstract class FirestoreFieldExpression : Expression, IExtensionExpression
{
    private static readonly MethodInfo _mGetValue;

    private static readonly MethodInfo _mContainsField;

    private static readonly PropertyInfo _pDocumentSnapshotId;

    static FirestoreFieldExpression()
    {
        Expression<Func<DocumentSnapshot, FieldPath, Value>> e0 = (doc, name) => doc.GetValue<Value>(name);
        _mGetValue = ((MethodCallExpression)e0.Body).Method;
        Expression<Func<DocumentSnapshot, FieldPath, bool>> e1 = (doc, name) => doc.ContainsField(name);
        _mContainsField = ((MethodCallExpression)e1.Body).Method;
        Expression<Func<DocumentSnapshot, string>> e2 = doc => doc.Id;
        _pDocumentSnapshotId = (PropertyInfo)((MemberExpression)e2.Body).Member;
    }

    public override bool CanReduce => true;

    public override ExpressionType NodeType => ExpressionType.Extension;

    public override Type Type { get; }

    // FIXME: consider using single string as there is no use case where this field holds more than a single
    // value... At least right now...
    public ImmutableList<string>? RawPath { get; }

    public FieldPath Path { get; }

    public Expression Instance { get; }

    public FirestoreConverter Converter { get; }

    protected FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string>? rawPath, FieldPath path, Type type)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Converter = converter ?? throw new ArgumentNullException(nameof(converter));
        RawPath = rawPath;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        if (instance.Type != typeof(DocumentSnapshot))
        {
            throw new InvalidOperationException("Firestore field expression can only be created for document snapshots.");
        }
    }

    protected abstract Expression ReduceToNonExtension();

    public override Expression Reduce()
    {
        if (Path.Equals(FieldPath.DocumentId))
        {
            return Property(Instance, _pDocumentSnapshotId);
        }
        return ReduceToNonExtension();
    }

    public override string ToString()
        => $"{Instance}[{Path}]";

    public abstract Expression AcceptNoReduce(ExpressionVisitor visitor);
}

public class FirestoreFieldExpression<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : FirestoreFieldExpression
{
    private static readonly Expression<Func<DocumentSnapshot, FirestoreConverter, FieldPath, T>> _template
        = (doc, converter, path) => converter.ConvertFromValue<T>(
            doc.ContainsField(path)
                ? doc.GetValue<Value>(path)
                : new Value { NullValue = default }
        );

    protected override Expression ReduceToNonExtension()
    {
        return _template.Body
            .SubstituteParameter(_template.Parameters[0], Instance)
            .SubstituteParameter(_template.Parameters[1], Constant(Converter))
            .SubstituteParameter(_template.Parameters[2], Constant(Path));
    }

    private FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string>? rawPath, FieldPath path)
        : base(converter, instance, rawPath, path, typeof(T))
    { }

    public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string> rawPath)
        : this(
            converter,
            instance,
            rawPath ?? throw new ArgumentNullException(nameof(rawPath), "For special paths use overloaded constructor."),
            new FieldPath([.. rawPath]))
    { }

    public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, FieldPath specialPath)
        : this(converter, instance, default, specialPath)
    { }

    public override Expression AcceptNoReduce(ExpressionVisitor visitor)
    {
        var newInstance = visitor.Visit(Instance);
        return ReferenceEquals(newInstance, Instance)
            ? this
            : new FirestoreFieldExpression<T>(Converter, newInstance, RawPath, Path);
    }
}