import {
  Accordion,
  Avatar,
  Badge,
  Box,
  Center,
  Grid,
  Group,
  Input,
  Pagination,
  ScrollArea,
  Select,
  Stack,
  Text,
  TextInput,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { createStyles } from '@mantine/emotion'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountGroupOutline,
  mdiAccountOutline,
  mdiBadgeAccountHorizontalOutline,
  mdiCheck,
  mdiClose,
  mdiEmailOutline,
  mdiIdentifier,
  mdiPhoneOutline,
  mdiStar,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { ParticipationStatusControl } from '@Components/admin/ParticipationStatusControl'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useParticipationStatusMap } from '@Utils/Shared'
import { useAccordionStyles } from '@Utils/ThemeOverride'
import api, { ParticipationInfoModel, ParticipationStatus, ProfileUserInfoModel } from '@Api'

interface MemberItemProps {
  user: ProfileUserInfoModel
  isRegistered: boolean
  isCaptain: boolean
}

const iconProps = {
  size: 0.9,
  color: 'gray',
}

const useGridStyles = createStyles((theme) => ({
  root: {
    flexDirection: 'row',
    flexGrow: 1,
    gap: 0,
  },

  col: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-start',
    gap: theme.spacing.xs,
    boxSizing: 'border-box',
    padding: `0 ${theme.spacing.xs}`,
    height: '1.5rem',
  },

  input: {
    userSelect: 'none',
    lineHeight: 1,
    fontSize: '1rem',
  },
}))

const MemberItem: FC<MemberItemProps> = (props) => {
  const { user, isCaptain, isRegistered } = props
  const theme = useMantineTheme()

  const { t } = useTranslation()
  const { classes } = useGridStyles()

  return (
    <Group wrap="nowrap" gap="xl" justify="space-between">
      <Group wrap="nowrap" w="calc(100% - 16rem)" miw="500px">
        <Avatar alt="avatar" src={user.avatar}>
          {user.userName?.slice(0, 1) ?? 'U'}
        </Avatar>
        <Grid className={classes.root}>
          <Grid.Col span={3} className={classes.col}>
            <Icon path={mdiIdentifier} {...iconProps} />
            <Text fw="bold">{user.userName}</Text>
          </Grid.Col>
          <Grid.Col span={3} className={classes.col}>
            <Icon path={mdiBadgeAccountHorizontalOutline} {...iconProps} />
            <Input
              variant="unstyled"
              value={user.stdNumber || t('admin.placeholder.empty')}
              readOnly
              classNames={{ input: classes.input }}
            />
          </Grid.Col>
          <Grid.Col span={6} className={classes.col}>
            <Icon path={mdiEmailOutline} {...iconProps} />
            <Text>{user.email || t('admin.placeholder.empty')}</Text>
          </Grid.Col>
          <Grid.Col span={6} className={classes.col}>
            <Icon path={mdiAccountOutline} {...iconProps} />
            <Input
              variant="unstyled"
              value={user.realName || t('admin.placeholder.empty')}
              readOnly
              classNames={{ input: classes.input }}
            />
          </Grid.Col>
          <Grid.Col span={6} className={classes.col}>
            <Icon path={mdiPhoneOutline} {...iconProps} />
            <Text>{user.phone || t('admin.placeholder.empty')}</Text>
          </Grid.Col>
        </Grid>
      </Group>
      <Group wrap="nowrap" justify="right">
        {isCaptain && (
          <Group gap={0}>
            <Icon path={mdiStar} color={theme.colors.yellow[4]} size={0.9} />
            <Text size="sm" fw={500} c="yellow">
              {t('team.content.role.captain')}
            </Text>
          </Group>
        )}
        <Text size="sm" fw="bold" c={isRegistered ? 'teal' : 'orange'}>
          {isRegistered
            ? t('admin.content.games.review.participation.joined')
            : t('admin.content.games.review.participation.not_joined')}
        </Text>
      </Group>
    </Group>
  )
}

interface ParticipationItemProps {
  participation: ParticipationInfoModel
  disabled: boolean
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

const ParticipationItem: FC<ParticipationItemProps> = (props) => {
  const { participation, disabled, setParticipationStatus } = props
  const part = useParticipationStatusMap().get(participation.status!)!

  const { t } = useTranslation()

  return (
    <Accordion.Item value={participation.id!.toString()}>
      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Accordion.Control>
          <Group justify="space-between">
            <Group>
              <Avatar alt="avatar" src={participation.team?.avatar}>
                {!participation.team?.name ? 'T' : participation.team.name.slice(0, 1)}
              </Avatar>
              <Box>
                <Text truncate fw={500}>
                  {!participation.team?.name
                    ? t('admin.placeholder.games.participation.team')
                    : participation.team.name}
                </Text>
                <Text truncate size="sm" c="dimmed">
                  {!participation.team?.bio
                    ? t('admin.placeholder.games.participation.bio')
                    : participation.team.bio}
                </Text>
              </Box>
            </Group>
            <Group wrap="nowrap" justify="space-between" w="32%" miw="350px">
              <Box>
                <Text>{participation.organization}</Text>
                <Text size="sm" c="dimmed" fw="bold">
                  {t('admin.content.games.review.participation.stats', {
                    count: participation.registeredMembers?.length ?? 0,
                    total: participation.team?.members?.length ?? 0,
                  })}
                </Text>
              </Box>
              <Center w="6em">
                <Badge color={part.color}>{part.title}</Badge>
              </Center>
              <ParticipationStatusControl
                disabled={disabled}
                participateId={participation.id!}
                status={participation.status!}
                setParticipationStatus={setParticipationStatus}
              />
            </Group>
          </Group>
        </Accordion.Control>
      </Box>
      <Accordion.Panel>
        <Stack>
          {participation.team?.members?.map((user) => (
            <MemberItem
              key={user.userId}
              user={user}
              isRegistered={
                participation.registeredMembers?.some((u) => u === user.userId) ?? false
              }
              isCaptain={participation.team?.captainId === user.userId}
            />
          ))}
        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  )
}

const PART_NUM_PER_PAGE = 10

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [disabled, setDisabled] = useState(false)
  const [selectedStatus, setSelectedStatus] = useState<ParticipationStatus | null>(null)
  const [selectedOrg, setSelectedOrg] = useState<string | null>(null)
  const [participations, setParticipations] = useState<ParticipationInfoModel[]>()
  const [search, setSearch] = useInputState('')
  const { classes } = useAccordionStyles()
  const participationStatusMap = useParticipationStatusMap()

  const { t } = useTranslation()
  const [activePage, setPage] = useState(1)

  const setParticipationStatus = async (id: number, status: ParticipationStatus) => {
    setDisabled(true)
    try {
      await api.admin.adminParticipation(id, status)
      setParticipations(
        participations?.map((value) => (value.id === id ? { ...value, status } : value))
      )
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.participation.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (err: any) {
      showErrorNotification(err, t)
    } finally {
      setDisabled(false)
    }
  }

  useEffect(() => {
    setPage(1)
  }, [selectedStatus, selectedOrg, search])

  useEffect(() => {
    if (numId < 0) {
      showNotification({
        color: 'red',
        message: t('common.error.param_error'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    api.game.gameParticipations(numId).then((res) => {
      setParticipations(res.data)
    })
  }, [])

  const orgs = Array.from(new Set(participations?.map((p) => p.organization ?? '') ?? [])).filter(
    (org) => !!org
  )

  const filteredParticipations = participations?.filter(
    (participation) =>
      (selectedStatus === null || participation.status === selectedStatus) &&
      (selectedOrg === null || participation.organization === selectedOrg) &&
      (search === '' || participation.team?.name?.toLowerCase().includes(search.toLowerCase()))
  )

  const pagedParticipations = filteredParticipations?.slice(
    (activePage - 1) * PART_NUM_PER_PAGE,
    activePage * PART_NUM_PER_PAGE
  )

  return (
    <WithGameEditTab
      headProps={{ justify: 'apart' }}
      isLoading={!participations}
      head={
        <Group justify="space-between" wrap="nowrap" w="100%">
          <TextInput
            w="20rem"
            placeholder={t('admin.placeholder.teams.search')}
            value={search}
            onChange={setSearch}
            rightSection={<Icon path={mdiAccountGroupOutline} size={1} />}
          />
          <Group justify="right" wrap="nowrap">
            {orgs.length && (
              <Select
                placeholder={t('admin.content.show_all')}
                clearable
                data={orgs.map((org) => ({ value: org, label: org }))}
                value={selectedOrg}
                onChange={(value) => setSelectedOrg(value)}
              />
            )}
            <Select
              placeholder={t('admin.content.show_all')}
              clearable
              data={Array.from(participationStatusMap, (v) => ({ value: v[0], label: v[1].title }))}
              value={selectedStatus}
              onChange={(value) => setSelectedStatus(value as ParticipationStatus | null)}
            />
          </Group>
        </Group>
      }
    >
      <ScrollArea type="never" pos="relative" h="calc(100vh - 250px)">
        {!participations || participations.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.review.empty.title')}</Title>
              <Text>{t('admin.content.games.review.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Accordion
            variant="contained"
            chevronPosition="left"
            classNames={classes}
            className={classes.root}
          >
            {pagedParticipations?.map((participation) => (
              <ParticipationItem
                key={participation.id}
                participation={participation}
                disabled={disabled}
                setParticipationStatus={setParticipationStatus}
              />
            ))}
          </Accordion>
        )}
      </ScrollArea>
      <Pagination
        value={activePage}
        onChange={setPage}
        total={(filteredParticipations?.length ?? 0) / PART_NUM_PER_PAGE + 1}
        styles={{
          root: {
            display: 'flex',
            justifyContent: 'flex-end',
            flexDirection: 'row',
          },
        }}
      />
    </WithGameEditTab>
  )
}

export default GameTeamReview
