import {
  Accordion,
  ActionIcon,
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
  mdiPencil,
  mdiPhoneOutline,
  mdiStar,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { DivisionEditModal } from '@Components/admin/DivisionEditModal'
import { ParticipationStatusControl } from '@Components/admin/ParticipationStatusControl'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useParticipationStatusMap } from '@Utils/Shared'
import { useAdminGame } from '@Hooks/useGame'
import api, { ParticipationEditModel, ParticipationInfoModel, ParticipationStatus, ProfileUserInfoModel } from '@Api'
import classes from '@Styles/Accordion.module.css'
import misc from '@Styles/Misc.module.css'
import reviewClasses from '@Styles/Review.module.css'

interface MemberItemProps {
  user: ProfileUserInfoModel
  isRegistered: boolean
  isCaptain: boolean
}

const iconProps = {
  size: 0.9,
  color: 'gray',
}

const MemberItem: FC<MemberItemProps> = (props) => {
  const { user, isCaptain, isRegistered } = props
  const theme = useMantineTheme()

  const { t } = useTranslation()

  return (
    <Group wrap="nowrap" gap="xl" justify="space-between">
      <Group wrap="nowrap" w="calc(100% - 16rem)" miw="500px">
        <Avatar alt="avatar" src={user.avatar}>
          {user.userName?.slice(0, 1) ?? 'U'}
        </Avatar>
        <Grid className={reviewClasses.root}>
          <Grid.Col span={3} className={reviewClasses.col}>
            <Icon path={mdiIdentifier} {...iconProps} />
            <Text fw="bold">{user.userName}</Text>
          </Grid.Col>
          <Grid.Col span={3} className={reviewClasses.col}>
            <Icon path={mdiBadgeAccountHorizontalOutline} {...iconProps} />
            <Input
              variant="unstyled"
              value={user.stdNumber || t('admin.placeholder.empty')}
              readOnly
              classNames={{ input: reviewClasses.input }}
            />
          </Grid.Col>
          <Grid.Col span={6} className={reviewClasses.col}>
            <Icon path={mdiEmailOutline} {...iconProps} />
            <Text>{user.email || t('admin.placeholder.empty')}</Text>
          </Grid.Col>
          <Grid.Col span={6} className={reviewClasses.col}>
            <Icon path={mdiAccountOutline} {...iconProps} />
            <Input
              variant="unstyled"
              value={user.realName || t('admin.placeholder.empty')}
              readOnly
              classNames={{ input: reviewClasses.input }}
            />
          </Grid.Col>
          <Grid.Col span={6} className={reviewClasses.col}>
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
  onEditDiv: () => void
  setParticipation: (id: number, model: ParticipationEditModel) => Promise<void>
}

const ParticipationItem: FC<ParticipationItemProps> = (props) => {
  const { participation, disabled, onEditDiv, setParticipation } = props
  const part = useParticipationStatusMap().get(participation.status!)!

  const { t } = useTranslation()

  return (
    <Accordion.Item value={participation.id!.toString()}>
      <Box className={misc.alignCenter} display="flex">
        <Accordion.Control>
          <Group justify="space-between" wrap="nowrap">
            <Group wrap="nowrap">
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
                  {!participation.team?.bio ? t('admin.placeholder.games.participation.bio') : participation.team.bio}
                </Text>
              </Box>
            </Group>
            <Group wrap="nowrap" justify="space-between" w="35%" miw="370px">
              <Box w="10em">
                {participation.division && (
                  <Group gap={0} wrap="nowrap">
                    <Text fw={500} truncate>
                      {participation.division}
                    </Text>
                    <ActionIcon size="sm" onClick={onEditDiv} disabled={disabled}>
                      <Icon path={mdiPencil} size={0.6} />
                    </ActionIcon>
                  </Group>
                )}
                <Text size="sm" c="dimmed" fw="bold">
                  {t('admin.content.games.review.participation.stats', {
                    count: participation.registeredMembers?.length ?? 0,
                    total: participation.team?.members?.length ?? 0,
                  })}
                </Text>
              </Box>
              <Center miw="5.5em">
                <Badge color={part.color}>{part.title}</Badge>
              </Center>
              <ParticipationStatusControl
                disabled={disabled}
                participateId={participation.id!}
                status={participation.status!}
                setParticipation={setParticipation}
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
              isRegistered={participation.registeredMembers?.some((u) => u === user.userId) ?? false}
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
  const { game } = useAdminGame(numId)

  const [disabled, setDisabled] = useState(false)
  const [selectedStatus, setSelectedStatus] = useState<ParticipationStatus | null>(null)
  const [selectedOrg, setSelectedOrg] = useState<string | null>(null)
  const [participations, setParticipations] = useState<ParticipationInfoModel[]>()
  const [search, setSearch] = useInputState('')
  const participationStatusMap = useParticipationStatusMap()

  const [divModalOpened, setDivModalOpened] = useState(false)
  const [curParticipation, setCurParticipation] = useState<ParticipationInfoModel | null>(null)

  const { t } = useTranslation()
  const [activePage, setPage] = useState(1)

  const setParticipation = async (id: number, model: ParticipationEditModel) => {
    setDisabled(true)
    try {
      await api.admin.adminParticipation(id, model)
      setParticipations(
        participations?.map((value) =>
          value.id === id
            ? {
                ...value,
                status: model.status ?? value.status,
                division: model.division ?? value.division,
              }
            : value
        )
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

    const fetchData = async () => {
      try {
        const res = await api.game.gameParticipations(numId)
        setParticipations(res.data)
      } catch (err: any) {
        showErrorNotification(err, t)
      }
    }

    fetchData()
  }, [navigate, numId, t])

  const divs = Array.from(new Set(participations?.map((p) => p.division ?? '') ?? [])).filter((div) => !!div)

  const filteredParticipations = participations?.filter(
    (participation) =>
      (selectedStatus === null || participation.status === selectedStatus) &&
      (selectedOrg === null || participation.division === selectedOrg) &&
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
            {divs.length && (
              <Select
                placeholder={t('admin.content.show_all')}
                clearable
                data={divs.map((div) => ({ value: div, label: div }))}
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
        {participations && participations.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.review.empty.title')}</Title>
              <Text>{t('admin.content.games.review.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Accordion variant="contained" chevronPosition="left" classNames={classes} className={classes.root}>
            {pagedParticipations?.map((participation) => (
              <ParticipationItem
                key={participation.id}
                participation={participation}
                disabled={disabled}
                onEditDiv={() => {
                  setCurParticipation(participation)
                  setDivModalOpened(true)
                }}
                setParticipation={setParticipation}
              />
            ))}
          </Accordion>
        )}
      </ScrollArea>
      <Pagination
        value={activePage}
        onChange={setPage}
        total={Math.ceil((filteredParticipations?.length ?? 1) / PART_NUM_PER_PAGE)}
        classNames={{
          root: cx(misc.flex, misc.flexRow, misc.justifyEnd),
        }}
      />
      {game?.divisions?.length && curParticipation && (
        <DivisionEditModal
          title={t('admin.content.games.review.edit_division')}
          opened={divModalOpened}
          divisions={game.divisions}
          participateId={curParticipation?.id ?? -1}
          currentDivision={curParticipation?.division ?? ''}
          setParticipation={setParticipation}
          onClose={() => {
            setDivModalOpened(false)
            setCurParticipation(null)
          }}
        />
      )}
    </WithGameEditTab>
  )
}

export default GameTeamReview
