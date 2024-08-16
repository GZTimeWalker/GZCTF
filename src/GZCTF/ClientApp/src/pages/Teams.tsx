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
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiAccountMultiplePlus, mdiCheck, mdiClose, mdiHumanGreetingVariant } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import LogoHeader from '@Components/LogoHeader'
import TeamCard from '@Components/TeamCard'
import TeamCreateModal from '@Components/TeamCreateModal'
import TeamEditModal from '@Components/TeamEditModal'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useIsMobile } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useTeams, useUser } from '@Utils/useUser'
import api, { Role, TeamInfoModel } from '@Api'

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
        title: t('common.error.encountered'),
        message: t('team.notification.join.wrong_invite_code'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    api.team
      .teamAccept(joinTeamCode)
      .then(() => {
        showNotification({
          color: 'teal',
          title: t('team.notification.join.success'),
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateTeams()
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setJoinTeamCode('')
        setJoinOpened(false)
      })
  }

  const { colorScheme } = useMantineColorScheme()

  usePageTitle(t('team.title.index'))

  const btns = (
    <>
      <Button
        leftSection={<Icon path={mdiHumanGreetingVariant} size={1} />}
        variant={colorScheme === 'dark' ? 'outline' : 'filled'}
        onClick={() => setJoinOpened(true)}
      >
        {t('team.button.join')}
      </Button>
      <Button
        leftSection={<Icon path={mdiAccountMultiplePlus} size={1} />}
        variant={colorScheme === 'dark' ? 'outline' : 'filled'}
        onClick={() => setCreateOpened(true)}
      >
        {t('team.button.create')}
      </Button>
    </>
  )

  return (
    <WithNavBar minWidth={0}>
      <WithRole requiredRole={Role.User}>
        <Stack pt="md">
          <Group justify={isMobile ? 'center' : 'space-between'} grow={isMobile}>
            {isMobile ? (
              btns
            ) : (
              <>
                <LogoHeader />
                <Group justify="right">{btns}</Group>
              </>
            )}
          </Group>
          {teams && !teamsError && user && !userError ? (
            <>
              <Title
                order={2}
                style={{
                  color: theme.colors[theme.primaryColor][colorScheme === 'dark' ? 2 : 6],
                  fontSize: '6rem',
                  fontWeight: 'bold',
                  opacity: 0.15,
                  height: '4.5rem',
                  paddingLeft: '1rem',
                  userSelect: 'none',
                  marginTop: '-1.5rem',
                }}
              >
                TEAMS
              </Title>
              <SimpleGrid
                cols={{ base: 1, lg: 2, w18: 3, w24: 4, w30: 5, w36: 6, w42: 7, w48: 8 }}
                spacing={{ base: 'sm', lg: 'md' }}
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

        <Modal
          opened={joinOpened}
          title={t('team.button.join')}
          onClose={() => setJoinOpened(false)}
        >
          <Stack>
            <Text size="sm">{t('team.content.join')}</Text>
            <TextInput
              label={t('team.label.invite_code')}
              type="text"
              placeholder="team:0:01234567890123456789012345678901"
              w="100%"
              value={joinTeamCode}
              onChange={(event) => setJoinTeamCode(event.currentTarget.value)}
            />
            <Button fullWidth variant="outline" onClick={onJoinTeam}>
              {t('team.button.join')}
            </Button>
          </Stack>
        </Modal>

        <TeamCreateModal
          opened={createOpened}
          title={t('team.button.create')}
          isOwnTeam={ownTeam ?? false}
          onClose={() => setCreateOpened(false)}
          mutate={mutateTeams}
        />

        <TeamEditModal
          opened={editOpened}
          title={t('team.button.edit')}
          onClose={() => setEditOpened(false)}
          team={editTeam}
          isCaptain={editTeam?.members?.some((m) => m?.captain && m.id === user?.userId) ?? false}
        />
      </WithRole>
    </WithNavBar>
  )
}

export default Teams
