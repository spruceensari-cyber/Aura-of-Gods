# Aura of Gods — Fair Matchmaking and Hero Claim Principles

## Core promise

Aura of Gods matchmaking must never deliberately assign weaker teammates to punish a player for winning.

Forbidden mechanics:
- forced-loss queues
- hidden bad-team assignment after win streaks
- 10-win or streak resets
- Elo gain suppression based on streak alone
- secret hero claim priority based on click speed

## Matchmaking order

1. Build candidate pools by Elo range.
2. Split teams to minimize average Elo difference.
3. Improve role coverage.
4. Balance rating uncertainty.
5. Expand the accepted Elo window gradually with queue time.

Win streaks may be displayed in profile statistics, but they must not reduce match quality or change Elo gain/loss.

## Hero claim mode

This mode is ban-free. Every player may claim any hero.

When multiple players in the same match claim the same hero, click speed is ignored. Priority uses proven hero performance:

- Strength-adjusted win performance: 55%
- Raw win rate: 15%
- Role-adjusted impact: 12%
- KDA quality: 8%
- Objective participation: 5%
- Sample confidence: 3%
- Recent form: 2%

Win outcomes therefore control 70% of the score. Recent form is a small hero-claim signal only and must never alter matchmaking team quality.

## Strength adjustment

Expected win probability is calculated from player Elo and opponent average Elo. Actual wins are compared with expected wins.

Beating stronger opponents increases strength-adjusted win performance. Farming weak opponents does not receive the same value.

## Role-adjusted impact

Different roles are scored by different responsibilities.

Support:
- vision impact
- ally saves
- kill participation
- objective participation
- crowd control duration
- roam impact

Jungle:
- objective control
- jungle control
- kill participation
- roam impact
- lane pressure
- vision impact

Mid:
- damage share
- kill participation
- roam impact
- objective participation
- lane pressure
- gold efficiency

Bot/Marksman:
- damage share
- gold efficiency
- kill participation
- tower damage
- objective participation
- survival efficiency

Top:
- lane pressure
- damage absorbed
- tower damage
- objective participation
- damage share
- kill participation

## Anti-stat-padding rule

KDA is intentionally secondary. Players cannot win hero claims by avoiding team fights or farming safe statistics while failing to convert advantages into wins and objectives.

The system should continue evolving toward percentile normalization by role, rank band and patch version when sufficient live telemetry exists.
