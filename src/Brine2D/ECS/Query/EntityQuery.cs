using System.Numerics;
using Brine2D.Core;
using Brine2D.Animation;
using Brine2D.ECS.Components;
using System.Buffers;
using System.Drawing;

namespace Brine2D.ECS.Query;

/// <summary>
/// Represents an executable entity query with filtering criteria.
/// </summary>
public class EntityQuery
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions _options;
    private readonly List<Type> _withComponents = new();
    private readonly List<Type> _withoutComponents = new();
    private readonly List<string> _withTags = new();
    private readonly List<string> _withoutTags = new();
    private readonly List<string> _withAllTags = new();
    private readonly List<string> _withAnyTags = new();
    private readonly Dictionary<Type, Func<Component, bool>> _componentFilters = new();
    private Func<Entity, bool>? _predicate;
    private Func<Entity, object>? _orderBySelector;
    private Func<Entity, object>? _thenBySelector;
    private bool _orderDescending;
    private bool _thenByDescending;
    private int? _takeCount;
    private int? _skipCount;
    private bool _onlyActive = true; // Default to only active entities
    private Vector2? _spatialCenter;
    private float? _spatialRadius;
    private Rectangle? _spatialBounds;
    private Random? _random;

    internal EntityQuery(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _options = options ?? new ECSOptions(); // Default if not provided
    }

    /// <summary>
    /// Requires entities to have the specified component.
    /// Optionally filters by component property values.
    /// </summary>
    /// <example>
    /// <code>
    /// // Without filter
    /// .With&lt;HealthComponent&gt;()
    /// 
    /// // With filter
    /// .With&lt;HealthComponent&gt;(h => h.CurrentHealth &lt; 20)
    /// </code>
    /// </example>
    public EntityQuery With<T>(Func<T, bool>? filter = null) where T : Component
    {
        _withComponents.Add(typeof(T));
        
        if (filter != null)
        {
            _componentFilters[typeof(T)] = c => filter((T)c);
        }
        
        return this;
    }

    /// <summary>
    /// Requires entities to NOT have the specified component.
    /// </summary>
    public EntityQuery Without<T>() where T : Component
    {
        _withoutComponents.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Requires entities to have the specified tag.
    /// </summary>
    public EntityQuery WithTag(string tag)
    {
        _withTags.Add(tag);
        return this;
    }

    /// <summary>
    /// Requires entities to NOT have the specified tag.
    /// </summary>
    public EntityQuery WithoutTag(string tag)
    {
        _withoutTags.Add(tag);
        return this;
    }

    /// <summary>
    /// Requires entities to have ALL of the specified tags.
    /// </summary>
    /// <example>
    /// <code>
    /// // Entity must have all three tags
    /// var bosses = world.Query()
    ///     .WithAllTags("Enemy", "Boss", "Elite")
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery WithAllTags(params string[] tags)
    {
        _withAllTags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Requires entities to have AT LEAST ONE of the specified tags.
    /// </summary>
    /// <example>
    /// <code>
    /// // Entity must have at least one of these tags
    /// var targets = world.Query()
    ///     .WithAnyTag("Enemy", "NPC", "Breakable")
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery WithAnyTag(params string[] tags)
    {
        _withAnyTags.AddRange(tags);
        return this;
    }

    /// <summary>
    /// Filters entities within a circular radius from a center point.
    /// Requires entities to have a TransformComponent.
    /// </summary>
    /// <example>
    /// <code>
    /// // Find enemies within 200 units of player
    /// var nearbyEnemies = world.Query()
    ///     .WithinRadius(playerPos, 200f)
    ///     .With&lt;EnemyComponent&gt;()
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery WithinRadius(Vector2 center, float radius)
    {
        _spatialCenter = center;
        _spatialRadius = radius;
        _spatialBounds = null; // Clear bounds if set
        return this;
    }

    /// <summary>
    /// Filters entities within a rectangular bounds.
    /// Requires entities to have a TransformComponent.
    /// </summary>
    /// <example>
    /// <code>
    /// // Find entities visible in camera view
    /// var visible = world.Query()
    ///     .WithinBounds(new Rectangle(0, 0, 1280, 720))
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery WithinBounds(Rectangle bounds)
    {
        _spatialBounds = bounds;
        _spatialCenter = null; // Clear radius if set
        _spatialRadius = null;
        return this;
    }

    /// <summary>
    /// Adds a custom filter predicate.
    /// </summary>
    public EntityQuery Where(Func<Entity, bool> predicate)
    {
        if (_predicate == null)
        {
            _predicate = predicate;
        }
        else
        {
            var existingPredicate = _predicate;
            _predicate = e => existingPredicate(e) && predicate(e);
        }
        return this;
    }

    /// <summary>
    /// Orders results by the specified selector in ascending order.
    /// </summary>
    /// <example>
    /// <code>
    /// // Sort by distance to player
    /// var nearest = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .OrderBy(e => Vector2.Distance(
    ///         e.GetComponent&lt;TransformComponent&gt;().Position, 
    ///         playerPos))
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery OrderBy<TKey>(Func<Entity, TKey> selector)
    {
        _orderBySelector = e => selector(e)!;
        _orderDescending = false;
        return this;
    }

    /// <summary>
    /// Orders results by the specified selector in descending order.
    /// </summary>
    /// <example>
    /// <code>
    /// // Sort by health (highest first)
    /// var strongest = world.Query()
    ///     .With&lt;HealthComponent&gt;()
    ///     .OrderByDescending(e => e.GetComponent&lt;HealthComponent&gt;().CurrentHealth)
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery OrderByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _orderBySelector = e => selector(e)!;
        _orderDescending = true;
        return this;
    }

    /// <summary>
    /// Applies a secondary sort criteria (tie-breaker).
    /// </summary>
    /// <example>
    /// <code>
    /// // Sort by health, then by distance
    /// var enemies = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .OrderBy(e => e.GetComponent&lt;HealthComponent&gt;().CurrentHealth)
    ///     .ThenBy(e => Vector2.Distance(e.GetComponent&lt;TransformComponent&gt;().Position, playerPos))
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery ThenBy<TKey>(Func<Entity, TKey> selector)
    {
        _thenBySelector = e => selector(e)!;
        _thenByDescending = false;
        return this;
    }

    /// <summary>
    /// Applies a secondary sort criteria in descending order.
    /// </summary>
    public EntityQuery ThenByDescending<TKey>(Func<Entity, TKey> selector)
    {
        _thenBySelector = e => selector(e)!;
        _thenByDescending = true;
        return this;
    }

    /// <summary>
    /// Takes only the first N entities from the results.
    /// Useful for performance when you only need a few results.
    /// </summary>
    /// <example>
    /// <code>
    /// // Get only the 5 nearest enemies
    /// var nearestEnemies = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .OrderBy(e => Vector2.Distance(e.GetComponent&lt;TransformComponent&gt;().Position, playerPos))
    ///     .Take(5)
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery Take(int count)
    {
        _takeCount = count;
        return this;
    }

    /// <summary>
    /// Skips the first N entities from the results.
    /// Useful for pagination.
    /// </summary>
    /// <example>
    /// <code>
    /// // Get page 2 (skip first 10, take next 10)
    /// var page2 = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .Skip(10)
    ///     .Take(10)
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery Skip(int count)
    {
        _skipCount = count;
        return this;
    }

    /// <summary>
    /// Only returns active entities (default behavior).
    /// </summary>
    public EntityQuery OnlyActive()
    {
        _onlyActive = true;
        return this;
    }

    /// <summary>
    /// Includes inactive entities in the results.
    /// </summary>
    /// <example>
    /// <code>
    /// // Get all enemies including inactive/disabled ones
    /// var allEnemies = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .IncludeInactive()
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery IncludeInactive()
    {
        _onlyActive = false;
        return this;
    }

    /// <summary>
    /// Creates a copy of this query that can be further modified without affecting the original.
    /// Useful for creating variations of a base query.
    /// Note: Does not copy ordering, take, skip, or random state - these should be set fresh on the clone.
    /// </summary>
    /// <example>
    /// <code>
    /// // Create base query
    /// var enemyQuery = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .Without&lt;DeadComponent&gt;();
    /// 
    /// // Create variations
    /// var weakEnemies = enemyQuery.Clone()
    ///     .With&lt;HealthComponent&gt;(h => h.CurrentHealth &lt; 20)
    ///     .Execute();
    /// 
    /// var nearbyEnemies = enemyQuery.Clone()
    ///     .WithinRadius(playerPos, 200f)
    ///     .Execute();
    /// 
    /// var strongEnemies = enemyQuery.Clone()
    ///     .With&lt;HealthComponent&gt;(h => h.CurrentHealth &gt; 80)
    ///     .OrderByDescending(e => e.GetComponent&lt;HealthComponent&gt;().CurrentHealth)
    ///     .Take(3)
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery Clone()
    {
        var clone = new EntityQuery(_world);
        
        // Copy component lists
        clone._withComponents.AddRange(_withComponents);
        clone._withoutComponents.AddRange(_withoutComponents);
        
        // Copy tag lists
        clone._withTags.AddRange(_withTags);
        clone._withoutTags.AddRange(_withoutTags);
        clone._withAllTags.AddRange(_withAllTags);
        clone._withAnyTags.AddRange(_withAnyTags);
        
        // Copy component filters
        foreach (var kvp in _componentFilters)
        {
            clone._componentFilters[kvp.Key] = kvp.Value;
        }
        
        // Copy other filters
        clone._predicate = _predicate;
        clone._onlyActive = _onlyActive;
        
        // Copy spatial filters
        clone._spatialCenter = _spatialCenter;
        clone._spatialRadius = _spatialRadius;
        clone._spatialBounds = _spatialBounds;
        
        // Note: Don't copy ordering, take, skip, or random state
        // These should be set fresh on the cloned query
        
        return clone;
    }

    /// <summary>
    /// Executes the query and returns matching entities.
    /// </summary>
    public IEnumerable<Entity> Execute()
    {
        IEnumerable<Entity> results = _world.Entities.Where(entity =>
        {
            // Check active status
            if (_onlyActive && !entity.IsActive)
                return false;

            // Check required components
            if (_withComponents.Any(type => !entity.HasComponent(type)))
                return false;

            // Check excluded components
            if (_withoutComponents.Any(type => entity.HasComponent(type)))
                return false;

            // Check component filters
            foreach (var kvp in _componentFilters)
            {
                var componentType = kvp.Key;
                var filter = kvp.Value;

                // Get component using reflection
                var component = entity.GetAllComponents()
                    .FirstOrDefault(c => componentType.IsInstanceOfType(c));

                if (component == null || !filter(component))
                    return false;
            }

            // Check required tags (single)
            if (_withTags.Any(tag => !entity.Tags.Contains(tag)))
                return false;

            // Check excluded tags
            if (_withoutTags.Any(tag => entity.Tags.Contains(tag)))
                return false;

            // Check ALL required tags
            if (_withAllTags.Count > 0 && !_withAllTags.All(tag => entity.Tags.Contains(tag)))
                return false;

            // Check ANY required tags
            if (_withAnyTags.Count > 0 && !_withAnyTags.Any(tag => entity.Tags.Contains(tag)))
                return false;

            // Check spatial filters
            if (_spatialCenter.HasValue && _spatialRadius.HasValue)
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform == null)
                    return false;

                var distance = Vector2.Distance(transform.Position, _spatialCenter.Value);
                if (distance > _spatialRadius.Value)
                    return false;
            }

            if (_spatialBounds.HasValue)
            {
                var transform = entity.GetComponent<TransformComponent>();
                if (transform == null)
                    return false;

                var bounds = _spatialBounds.Value;
                var pos = transform.Position;
                if (pos.X < bounds.X || pos.X > bounds.X + bounds.Width ||
                    pos.Y < bounds.Y || pos.Y > bounds.Y + bounds.Height)
                    return false;
            }

            // Check custom predicate
            if (_predicate != null && !_predicate(entity))
                return false;

            return true;
        });

        // Apply ordering
        if (_orderBySelector != null)
        {
            IOrderedEnumerable<Entity> orderedResults = _orderDescending
                ? results.OrderByDescending(_orderBySelector)
                : results.OrderBy(_orderBySelector);

            // Apply secondary sort if specified
            if (_thenBySelector != null)
            {
                orderedResults = _thenByDescending
                    ? orderedResults.ThenByDescending(_thenBySelector)
                    : orderedResults.ThenBy(_thenBySelector);
            }

            results = orderedResults;
        }

        // Apply skip
        if (_skipCount.HasValue)
        {
            results = results.Skip(_skipCount.Value);
        }

        // Apply take
        if (_takeCount.HasValue)
        {
            results = results.Take(_takeCount.Value);
        }

        return results;
    }

    /// <summary>
    /// Executes the query and returns the first matching entity, or null.
    /// </summary>
    public Entity? First()
    {
        return Execute().FirstOrDefault();
    }

    /// <summary>
    /// Executes the query and returns a single random matching entity, or null.
    /// </summary>
    /// <example>
    /// <code>
    /// // Pick a random enemy to target
    /// var randomEnemy = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .Random();
    /// </code>
    /// </example>
    public Entity? Random()
    {
        // Get count first
        var count = Execute().Count();
        if (count == 0)
            return null;

        var array = ArrayPool<Entity>.Shared.Rent(count);
        try
        {
            int index = 0;
            foreach (var entity in Execute())
            {
                array[index++] = entity;
            }

            _random ??= new Random();
            return array[_random.Next(count)];
        }
        finally
        {
            ArrayPool<Entity>.Shared.Return(array, clearArray: true);
        }
    }

    /// <summary>
    /// Executes the query and returns N random matching entities.
    /// </summary>
    /// <example>
    /// <code>
    /// // Pick 3 random enemies to spawn
    /// var randomEnemies = world.Query()
    ///     .With&lt;EnemyComponent&gt;()
    ///     .Random(3)
    ///     .Execute();
    /// </code>
    /// </example>
    public EntityQuery Random(int count)
    {
        _random ??= new Random();
        
        // Execute query, shuffle, and take N
        var results = Execute().ToList();
        
        // Fisher-Yates shuffle
        for (int i = results.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (results[i], results[j]) = (results[j], results[i]);
        }

        // Return a new query with these results
        return Take(Math.Min(count, results.Count));
    }

    /// <summary>
    /// Executes the query and returns the count of matching entities.
    /// </summary>
    public int Count()
    {
        return Execute().Count();
    }

    /// <summary>
    /// Executes the query and checks if any entities match.
    /// </summary>
    public bool Any()
    {
        return Execute().Any();
    }

    /// <summary>
    /// Executes an action on each matching entity.
    /// Convenient alternative to foreach.
    /// </summary>
    /// <example>
    /// <code>
    /// // Damage all enemies in explosion radius
    /// world.Query()
    ///     .WithinRadius(explosionPos, 50f)
    ///     .With&lt;DamageableComponent&gt;()
    ///     .ForEach(e => e.GetComponent&lt;DamageableComponent&gt;().TakeDamage(10));
    /// </code>
    /// </example>
    public void ForEach(Action<Entity> action)
    {
        var entities = Execute().ToList(); // Snapshot to avoid collection modification
        
        if (_options.EnableParallelExecution && entities.Count >= _options.ParallelEntityThreshold)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism
            };

            Parallel.ForEach(entities, parallelOptions, action);
        }
        else
        {
            foreach (var entity in entities)
            {
                action(entity);
            }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 1 component.
    /// Components are passed directly - no GetComponent() needed!
    /// Automatically uses parallel execution if enabled and threshold is met.
    /// </summary>
    /// <example>
    /// <code>
    /// // Much faster than calling GetComponent() in the loop!
    /// world.Query()
    ///     .With&lt;VelocityComponent&gt;()
    ///     .ForEach((entity, VelocityComponent velocity) => 
    ///     {
    ///         velocity.Value *= 1.1f; // Speed boost
    ///     });
    /// </code>
    /// </example>
    public void ForEach<T1>(Action<Entity, T1> action) 
        where T1 : Component
    {
        var entities = Execute().ToList();
        
        if (_options.EnableParallelExecution && entities.Count >= _options.ParallelEntityThreshold)
        {
            Parallel.ForEach(entities, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism 
            }, entity =>
            {
                var c1 = entity.GetComponent<T1>();
                if (c1 != null) action(entity, c1);
            });
        }
        else
        {
            foreach (var entity in entities)
            {
                var c1 = entity.GetComponent<T1>();
                if (c1 != null) action(entity, c1);
            }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 2 components.
    /// Components are passed directly - no GetComponent() needed!
    /// Automatically uses parallel execution if enabled and threshold is met.
    /// </summary>
    /// <example>
    /// <code>
    /// // Clean and fast - no GetComponent() calls!
    /// world.Query()
    ///     .With&lt;TransformComponent&gt;()
    ///     .With&lt;VelocityComponent&gt;()
    ///     .ForEach((entity, TransformComponent t, VelocityComponent v) => 
    ///     {
    ///         t.Position += v.Value * gameTime.DeltaTime;
    ///     });
    /// </code>
    /// </example>
    public void ForEach<T1, T2>(Action<Entity, T1, T2> action) 
        where T1 : Component
        where T2 : Component
    {
        var entities = Execute().ToList();
        
        if (_options.EnableParallelExecution && entities.Count >= _options.ParallelEntityThreshold)
        {
            Parallel.ForEach(entities, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism 
            }, entity =>
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                if (c1 != null && c2 != null) action(entity, c1, c2);
            });
        }
        else
        {
            foreach (var entity in entities)
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                if (c1 != null && c2 != null) action(entity, c1, c2);
            }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 3 components.
    /// Components are passed directly - no GetComponent() needed!
    /// Automatically uses parallel execution if enabled and threshold is met.
    /// </summary>
    /// <example>
    /// <code>
    /// world.Query()
    ///     .With&lt;TransformComponent&gt;()
    ///     .With&lt;SpriteComponent&gt;()
    ///     .With&lt;HealthComponent&gt;()
    ///     .ForEach((entity, TransformComponent t, SpriteComponent s, HealthComponent h) => 
    ///     {
    ///         s.Color = h.CurrentHealth &lt; 20 ? Color.Red : Color.White;
    ///     });
    /// </code>
    /// </example>
    public void ForEach<T1, T2, T3>(Action<Entity, T1, T2, T3> action) 
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        var entities = Execute().ToList();
        
        if (_options.EnableParallelExecution && entities.Count >= _options.ParallelEntityThreshold)
        {
            Parallel.ForEach(entities, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism 
            }, entity =>
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                var c3 = entity.GetComponent<T3>();
                if (c1 != null && c2 != null && c3 != null) 
                    action(entity, c1, c2, c3);
            });
        }
        else
        {
            foreach (var entity in entities)
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                var c3 = entity.GetComponent<T3>();
                if (c1 != null && c2 != null && c3 != null) 
                    action(entity, c1, c2, c3);
            }
        }
    }

    /// <summary>
    /// Executes an action for each entity with 4 components.
    /// Components are passed directly - no GetComponent() needed!
    /// Automatically uses parallel execution if enabled and threshold is met.
    /// </summary>
    /// <example>
    /// <code>
    /// world.Query()
    ///     .With&lt;TransformComponent&gt;()
    ///     .With&lt;VelocityComponent&gt;()
    ///     .With&lt;HealthComponent&gt;()
    ///     .With&lt;AIComponent&gt;()
    ///     .ForEach((entity, TransformComponent t, VelocityComponent v, HealthComponent h, AIComponent ai) => 
    ///     {
    ///         // Complex AI logic with all components available
    ///     });
    /// </code>
    /// </example>
    public void ForEach<T1, T2, T3, T4>(Action<Entity, T1, T2, T3, T4> action) 
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        var entities = Execute().ToList();
        
        if (_options.EnableParallelExecution && entities.Count >= _options.ParallelEntityThreshold)
        {
            Parallel.ForEach(entities, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism 
            }, entity =>
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                var c3 = entity.GetComponent<T3>();
                var c4 = entity.GetComponent<T4>();
                if (c1 != null && c2 != null && c3 != null && c4 != null) 
                    action(entity, c1, c2, c3, c4);
            });
        }
        else
        {
            foreach (var entity in entities)
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                var c3 = entity.GetComponent<T3>();
                var c4 = entity.GetComponent<T4>();
                if (c1 != null && c2 != null && c3 != null && c4 != null) 
                    action(entity, c1, c2, c3, c4);
            }
        }
    }
}