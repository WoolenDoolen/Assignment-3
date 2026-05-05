using UnityEngine;

public class EnemyController : MonoBehaviour
{

    public Transform target;
    public int speed;
    public int attackDamage;
    public Hittable hp;
    public HealthBar healthui;
    public bool dead;

    public float last_attack;
    private Unit unit;
    private Vector3 last_position;
    private float stuck_time;
    private int turn_direction = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unit = GetComponent<Unit>();
        last_position = transform.position;
        target = GameManager.Instance.player.transform;
        if (hp == null)
        {
            hp = new Hittable(50, Hittable.Team.MONSTERS, gameObject);
        }
        hp.OnDeath += Die;
        healthui.SetHealth(hp);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state != GameManager.GameState.INWAVE || target == null)
        {
            unit.movement = Vector2.zero;
            return;
        }

        Vector3 direction = target.position - transform.position;
        if (direction.magnitude < 2f)
        {
            unit.movement = Vector2.zero;
            DoAttack();
        }
        else
        {
            unit.movement = PickMoveDirection(direction) * speed;
            TrackStuck();
        }
    }

    Vector2 PickMoveDirection(Vector2 direction)
    {
        Vector2 desired = direction.normalized;
        float testDistance = speed * Time.fixedDeltaTime * 1.5f;
        int side = stuck_time > 0.4f ? turn_direction : 1;
        float[] angles = { 0, 35 * side, -35 * side, 70 * side, -70 * side, 110 * side, -110 * side, 180 };

        foreach (float angle in angles)
        {
            Vector2 candidate = Quaternion.Euler(0, 0, angle) * desired;
            if (unit.CanMove(candidate * testDistance))
            {
                return candidate.normalized;
            }
        }
        return desired;
    }

    void TrackStuck()
    {
        if (Vector3.Distance(transform.position, last_position) < 0.02f)
        {
            stuck_time += Time.deltaTime;
            if (stuck_time > 0.8f)
            {
                turn_direction *= -1;
                stuck_time = 0.4f;
            }
        }
        else
        {
            stuck_time = 0;
            last_position = transform.position;
        }
    }
    
    void DoAttack()
    {
        if (last_attack + 2 < Time.time)
        {
            last_attack = Time.time;
            int dmg = attackDamage > 0 ? attackDamage : 5;
            PlayerController player = target.gameObject.GetComponent<PlayerController>();
            if (player.hp != null)
            {
                player.hp.Damage(new Damage(dmg, Damage.Type.PHYSICAL));
            }
        }
    }


    void Die()
    {
        if (!dead)
        {
            dead = true;
            GameManager.Instance.RemoveEnemy(gameObject);
            Destroy(gameObject);
        }
    }
}
