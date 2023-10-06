import { FC, useState } from 'react'
import {
  Button,
  Center,
  Group,
  Loader,
  Modal,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiAccountMultiplePlus, mdiCheck, mdiClose, mdiHumanGreetingVariant } from '@mdi/js'
import { Icon } from '@mdi/react'
import LogoHeader from '@Components/LogoHeader'
import TeamCard from '@Components/TeamCard'
import TeamCreateModal from '@Components/TeamCreateModal'
import TeamEditModal from '@Components/TeamEditModal'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useIsMobile } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useTeams, useUser } from '@Utils/useUser'
import api, { Role, TeamInfoModel } from '@Api'
import { useTranslation } from 'react-i18next'
import i18nKeyOf from '../utils/I18n'

const Teams: FC = () => {
  const { user, error: userError } = useUser()
  const { teams, mutate: mutateTeams, error: teamsError } = useTeams()

  const theme = useMantineTheme()

  const [joinOpened, setJoinOpened] = useState(false)
  const [joinTeamCode, setJoinTeamCode] = useState('')

  const [createOpened, setCreateOpened] = useState(false)

  const [editOpened, setEditOpened] = useState(false)
  const [editTeam, setEditTeam] = useState<TeamInfoModel | null>(null)

  const ownTeam = teams?.some((t) => t.members?.some((m) => m?.captain && m.id === user?.userId))

  const isMobile = useIsMobile()

  const { t } = useTranslation()

  const onEditTeam = (team: TeamInfoModel) => {
    setEditTeam(team)
    setEditOpened(true)
  }

  const codePartten = /:\d+:[0-9a-f]{32}$/

  const onJoinTeam = () => {
    if (!codePartten.test(joinTeamCode)) {
      showNotification({
        color: 'red',
        title: t(i18nKeyOf('ErrorEncountered')),
        message: t(i18nKeyOf('Team_InvalidInvitationCodeFormat')),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    api.team
      .teamAccept(joinTeamCode)
      .then(() => {
        showNotification({
          color: 'teal',
          title: t(i18nKeyOf('Team_Joined')),
          message: t(i18nKeyOf('Team_Updated')),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateTeams()
      })
      .catch(showErrorNotification)
      .finally(() => {
        setJoinTeamCode('')
        setJoinOpened(false)
      })
  }

  usePageTitle('队伍管理')

  const btns = (
    <>
      <Button
        leftIcon={<Icon path={mdiHumanGreetingVariant} size={1} />}
        variant={theme.colorScheme === 'dark' ? 'outline' : 'filled'}
        onClick={() => setJoinOpened(true)}
      >
        加入队伍
      </Button>
      <Button
        leftIcon={<Icon path={mdiAccountMultiplePlus} size={1} />}
        variant={theme.colorScheme === 'dark' ? 'outline' : 'filled'}
        onClick={() => setCreateOpened(true)}
      >
        创建队伍
      </Button>
    </>
  )

  return (
    <WithNavBar minWidth={0}>
      <WithRole requiredRole={Role.User}>
        <Stack pt="md">
          <Group position={isMobile ? 'center' : 'apart'} grow={isMobile}>
            {isMobile ? (
              btns
            ) : (
              <>
                <LogoHeader />
                <Group position="right">{btns}</Group>
              </>
            )}
          </Group>
          {teams && !teamsError && user && !userError ? (
            <>
              <Title
                order={2}
                style={{
                  fontSize: '6rem',
                  fontWeight: 'bold',
                  opacity: 0.15,
                  height: '4.5rem',
                  paddingLeft: '1rem',
                  color: theme.colors.brand[theme.colorScheme === 'dark' ? 2 : 6],
                  userSelect: 'none',
                  marginTop: '-1.5rem',
                }}
              >
                TEAMS
              </Title>
              <SimpleGrid
                cols={3}
                spacing="lg"
                breakpoints={[
                  { maxWidth: 1600, cols: 2, spacing: 'md' },
                  { maxWidth: 800, cols: 1, spacing: 'sm' },
                ]}
              >
                {teams.map((t, i) => (
                  <TeamCard
                    key={i}
                    team={t}
                    isCaptain={t.members?.some((m) => m?.captain && m.id === user?.userId) ?? false}
                    onEdit={() => onEditTeam(t)}
                  />
                ))}
              </SimpleGrid>
            </>
          ) : (
            <Center w="100%" h="80wh">
              <Loader />
            </Center>
          )}
        </Stack>

        <Modal opened={joinOpened} title="加入已有队伍" onClose={() => setJoinOpened(false)}>
          <Stack>
            <Text size="sm">请从队伍创建者处获取队伍邀请码，输入邀请码加入队伍。</Text>
            <TextInput
              label="邀请码"
              type="text"
              placeholder="team:0:01234567890123456789012345678901"
              w="100%"
              value={joinTeamCode}
              onChange={(event) => setJoinTeamCode(event.currentTarget.value)}
            />
            <Button fullWidth variant="outline" onClick={onJoinTeam}>
              加入队伍
            </Button>
          </Stack>
        </Modal>

        <TeamCreateModal
          opened={createOpened}
          title="创建新队伍"
          isOwnTeam={ownTeam ?? false}
          onClose={() => setCreateOpened(false)}
          mutate={mutateTeams}
        />

        <TeamEditModal
          opened={editOpened}
          title="队伍详情"
          onClose={() => setEditOpened(false)}
          team={editTeam}
          isCaptain={editTeam?.members?.some((m) => m?.captain && m.id === user?.userId) ?? false}
        />
      </WithRole>
    </WithNavBar>
  )
}

export default Teams
