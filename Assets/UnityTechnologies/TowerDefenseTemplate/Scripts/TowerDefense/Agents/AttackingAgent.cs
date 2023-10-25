using Core.Health;
using TowerDefense.Affectors;
using TowerDefense.Towers;
using UnityEngine;
using System.Collections;
using TowerDefense.Level;

namespace TowerDefense.Agents
{
	/// <summary>
	/// An implementation of Agent that will attack 
	/// any Towers that block its path
	/// </summary>
	public class AttackingAgent : Agent
	{
		/// <summary>
		/// Tower to target
		/// </summary>
		protected Tower m_TargetTower;

		/// <summary>
		/// The attached attack affector
		/// </summary>
		protected AttackAffector m_AttackAffector;
		
		/// <summary>
		/// Is this agent currently engaging a tower?
		/// </summary>
		protected bool m_IsAttacking;

		[SerializeField] Animator animator;

		public override void Initialize()
		{
			base.Initialize();
			
			// Attack affector
			m_AttackAffector.Initialize(configuration.alignmentProvider);
			
			// We don't want agents to attack towers until their path is blocked, 
			// so disable m_AttackAffector until it is needed
			m_AttackAffector.enabled = false;
		}

		/// <summary>
		/// Unsubscribes from tracked towers removed event
		/// and disables the attached attack affector
		/// </summary>
		

		/// <summary>
		/// Gets the closest tower to the agent
		/// </summary>
		/// <returns>The closest tower</returns>
		protected Tower GetClosestTower()
		{
			var towerController = m_AttackAffector.towerTargetter.GetTarget() as Tower;
			return towerController;
		}

		/// <summary>
		/// Caches the Attack Affector if necessary
		/// </summary>
		protected override void LazyLoad()
		{
			base.LazyLoad();
			if (m_AttackAffector == null)
			{
				m_AttackAffector = GetComponent<AttackAffector>();
			}
		}
		
		/// <summary>
		/// If the tower is destroyed while other agents attack it, ensure it becomes null
		/// </summary>
		/// <param name="tower">The tower that has been destroyed</param>
		protected virtual void OnTargetTowerDestroyed(DamageableBehaviour tower)
		{
			if (m_TargetTower == tower)
			{
				m_TargetTower.removed -= OnTargetTowerDestroyed;
				m_TargetTower = null;
			}
		}

		/// <summary>
		/// Peforms the relevant path update for the states <see cref="Agent.State.OnCompletePath"/>, 
		/// <see cref="Agent.State.OnPartialPath"/> and <see cref="Agent.State.Attacking"/>
		/// </summary>
		protected override void PathUpdate()
		{
			if (animator)
			{
				animator.SetBool("isAttacking", m_IsAttacking);
				animator.SetBool("isDead", isDead);
			}
				

			if (isDead)
			{
				m_IsAttacking = false;

				m_TargetTower = null;
				m_AttackAffector.DisableFire();
				m_AttackAffector.enabled = false;

				m_NavMeshAgent.isStopped = true;
			}
			else
			{
				switch (state)
				{
					case State.OnCompletePath:
						OnCompletePathUpdate();
						break;
					case State.OnPartialPath:
						OnPartialPathUpdate();
						break;
					case State.Attacking:
						AttackingUpdate();
						break;
				}

				switch (state)
				{
					case State.OnCompletePath:
						m_AttackAffector.towerTargetter.returnToZeroPos = true;
						m_AttackAffector.towerTargetter.isAiming = false;
						break;
					case State.OnPartialPath:
						m_AttackAffector.towerTargetter.isAiming = false;
						break;
					case State.Attacking:
						m_AttackAffector.towerTargetter.isAiming = isPathBlocked ? true : m_LevelManager.isFiring;
						break;
				}
			}
		}

		/// <summary>
		/// Move along the path, change to <see cref="Agent.State.OnPartialPath" />
		/// </summary>
		protected override void OnCompletePathUpdate()
		{
			m_AttackAffector.towerTargetter.transform.position = transform.position;

			Tower tower = GetClosestTower();

			if (tower)
			{
				m_TargetTower = tower;
				float distanceToTower = Vector3.Distance(transform.position, m_TargetTower.transform.position);

				if(isPathBlocked || distanceToTower <= m_AttackAffector.towerTargetter.effectRadius)
				{
					if(LevelManager.instance.isFiring)
						state = State.OnPartialPath;
				}
				else
				{

					m_NavMeshAgent.isStopped = false;
					MoveToNode();
				}
			}
			else
			{

				m_TargetTower = null;
				m_NavMeshAgent.isStopped = false;
				MoveToNode();
			}

			


		}

		/// <summary>
		/// Change to <see cref="Agent.State.OnCompletePath" /> when path is no longer blocked or to
		/// <see cref="Agent.State.Attacking" /> when the agent reaches <see cref="AttackingAgent.m_TargetTower" />
		/// </summary>
		protected override void OnPartialPathUpdate()
		{
			// Check for closest tower at the end of the partial path
			//m_AttackAffector.towerTargetter.transform.position = m_NavMeshAgent.pathEndPosition;
			Tower tower = GetClosestTower();
			if (tower != m_TargetTower)
			{
				// if the current target is to be replaced, unsubscribe from removed event
				if (m_TargetTower != null)
				{
					m_TargetTower.removed -= OnTargetTowerDestroyed;
				}
				
				// assign target, can be null
				m_TargetTower = tower;
				
				// if new target found subscribe to removed event
				if (m_TargetTower != null)
				{
					m_TargetTower.removed += OnTargetTowerDestroyed;
				}
			}

			//if no tower left
			if (m_TargetTower == null)
			{
				//if not blocked path, return to complete path
				if (!isPathBlocked)
				{
					state = State.OnCompletePath;
					return;
				}
			}
			//if there is tower
			else
            {
				float distanceToTower = Vector3.Distance(transform.position, m_TargetTower.transform.position);
				//if too far, return
				if (distanceToTower > m_AttackAffector.towerTargetter.effectRadius)
				{
					state = State.OnCompletePath;
					return;
				}

				if (!m_AttackAffector.enabled)
				{
					m_AttackAffector.towerTargetter.transform.position = transform.position;
					m_AttackAffector.enabled = true;
				}

				m_AttackAffector.EnableFire();

				state = isPathBlocked ? State.Attacking : (m_LevelManager.isFiring? State.Attacking : State.OnCompletePath);
				m_NavMeshAgent.isStopped = true;
			}
			
		}
		
		/// <summary>
		/// The agent attacks until the path is available again or it has killed the target tower
		/// </summary>
		protected void AttackingUpdate()
		{
			m_IsAttacking = true;
			if (m_TargetTower)
			{
				return;
			}

			// Resume path once blocking has been cleared
			m_IsAttacking = false;

			m_TargetTower = null;
			m_AttackAffector.DisableFire();
			m_AttackAffector.enabled = false;

			Tower tower = GetClosestTower();
			if (tower)
			{
				float distanceToTower = Vector3.Distance(transform.position, tower.transform.position);
				state = (isPathBlocked || distanceToTower <= m_AttackAffector.towerTargetter.effectRadius) ? State.OnPartialPath : State.OnCompletePath;
			}
			else
			{
				state = State.OnCompletePath;
				MoveToNode();
			}

			// Move the Targetter back to the agent's position
			m_AttackAffector.towerTargetter.transform.position = transform.position;
		}

		public override void PlayDeathAnim()
		{

			StartCoroutine(deathAnim());
		}

		IEnumerator deathAnim()
		{
			yield return new WaitForSeconds(1);
			Remove();
		}
	}
}