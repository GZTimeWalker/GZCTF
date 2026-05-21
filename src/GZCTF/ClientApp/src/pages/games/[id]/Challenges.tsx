import { Button, Group, Stack } from '@mantine/core'
import { mdiOpenInNew, mdiSword } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ChallengePanel } from '@Components/ChallengePanel'
import { GameNoticePanel } from '@Components/GameNoticePanel'
import { TeamRank } from '@Components/TeamRank'
import { WithGameTab } from '@Components/WithGameTab'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { Role } from '@Api'

const Challenges: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { t } = useTranslation()

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <WithGameTab>
          <Group gap="sm" justify="space-between" align="flex-start" wrap="nowrap">
            <ChallengePanel />
            <Stack gap="sm" miw="22rem" maw="22rem">
              <Button
                component="a"
                href={`/games/${numId}/attack`}
                target="_blank"
                rel="noreferrer"
                variant="light"
                fullWidth
                leftSection={<Icon path={mdiSword} size={1} />}
                rightSection={<Icon path={mdiOpenInNew} size={0.8} />}
              >
                {t('game.button.attack')}
              </Button>
              <TeamRank />
              <GameNoticePanel />
            </Stack>
          </Group>
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default Challenges
