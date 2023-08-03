import dayjs from 'dayjs'
import { FC, forwardRef, useEffect, useState } from 'react'
import {
  Text,
  Select,
  Stack,
  useMantineTheme,
  Group,
  ScrollArea,
  Paper,
  Table,
  Badge,
  Box,
  Code,
  Tooltip,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountGroupOutline,
  mdiCheck,
  mdiChevronTripleRight,
  mdiPackageVariantClosedRemove,
  mdiPuzzleOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ChallengeTagLabelMap } from '@Utils/Shared'
import { useTableStyles, useTooltipStyles } from '@Utils/ThemeOverride'
import api, { ChallengeModel, ChallengeTag, TeamModel } from '@Api'

type SelectTeamItemProps = TeamModel & React.ComponentPropsWithoutRef<'div'>
type SelectChallengeItemProps = ChallengeModel & React.ComponentPropsWithoutRef<'div'>

const SelectTeamItem = forwardRef<HTMLDivElement, SelectTeamItemProps>(
  ({ name, id, ...others }: SelectTeamItemProps, ref) => (
    <Stack ref={ref} {...others} spacing={0}>
      <Text lineClamp={1} size="sm">
        {name}
      </Text>
      <Text size="xs" c="dimmed">
        {`#${id}`}
      </Text>
    </Stack>
  )
)

const SelectChallengeItem = forwardRef<HTMLDivElement, SelectChallengeItemProps>(
  ({ title, id, tag, ...others }: SelectChallengeItemProps, ref) => {
    const tagInfo = ChallengeTagLabelMap.get(tag ?? ChallengeTag.Misc)!
    const theme = useMantineTheme()

    return (
      <Group ref={ref} {...others}>
        <Icon color={theme.colors[tagInfo.color][4]} path={tagInfo.icon} size={1} />
        <Stack spacing={0}>
          <Text lineClamp={1} size="sm">
            {title}
          </Text>
          <Text size="xs" c="dimmed">
            {`#${id}`}
          </Text>
        </Stack>
      </Group>
    )
  }
)

const Instances: FC = () => {
  const { data: instances, mutate } = api.admin.useAdminInstances({
    refreshInterval: 30 * 1000, // refresh every 30 seconds
    revalidateOnFocus: false,
  })

  const [teams, setTeams] = useState<TeamModel[]>()
  const [challenge, setChallenge] = useState<ChallengeModel[]>()
  const [disabled, setDisabled] = useState(false)
  const { classes, theme } = useTableStyles()
  const clipBoard = useClipboard()
  const { classes: tooltipClasses } = useTooltipStyles()

  useEffect(() => {
    if (instances) {
      const teams = [
        ...new Map(instances.data.map((instance) => [instance.team!.id, instance.team!])).values(),
      ]
      setTeams(teams)

      const challenges = [
        ...new Map(
          instances.data.map((instance) => [instance.challenge!.id, instance.challenge!])
        ).values(),
      ]
      setChallenge(challenges)
    }
  }, [instances])

  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null)
  const [selectedChallengeId, setSelectedChallengeId] = useState<string | null>(null)

  const [filteredInstances, setFilteredInstances] = useState(instances?.data)

  useEffect(() => {
    if (!instances) return

    let filtered = instances.data

    if (selectedTeamId) {
      filtered = filtered.filter((instance) => instance.team?.id === Number(selectedTeamId))
    }

    if (selectedChallengeId) {
      filtered = filtered.filter(
        (instance) => instance.challenge?.id === Number(selectedChallengeId)
      )
    }

    setFilteredInstances(filtered)
  }, [instances, selectedTeamId, selectedChallengeId])

  const onDelete = async (instanceGuid?: string) => {
    if (!instanceGuid) return

    try {
      setDisabled(true)
      await api.admin.adminDestroyInstance(instanceGuid)

      showNotification({
        color: 'teal',
        message: '容器已销毁',
        icon: <Icon path={mdiCheck} size={1} />,
      })

      instances &&
        mutate({
          total: (instances.total ?? instances.length) - 1,
          length: instances.length - 1,
          data: instances.data.filter((instance) => instance.containerGuid !== instanceGuid),
        })
    } catch (e: any) {
      showErrorNotification(e)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AdminPage
      isLoading={!instances || !teams || !challenge}
      head={
        <>
          <Group w="60%" position="left" spacing="md">
            <Select
              w="48%"
              searchable
              clearable
              placeholder="所有队伍"
              value={selectedTeamId}
              onChange={(id) => setSelectedTeamId(id)}
              icon={<Icon path={mdiAccountGroupOutline} size={1} />}
              itemComponent={SelectTeamItem}
              data={
                teams?.map((team) => ({ value: String(team.id), label: team.name, ...team })) ?? []
              }
              filter={(query, team) => team.name.includes(query) || team.value.includes(query)}
              nothingFound="没有找到队伍"
            />
            <Select
              w="48%"
              searchable
              clearable
              placeholder="所有题目"
              value={selectedChallengeId}
              onChange={(id) => setSelectedChallengeId(id)}
              icon={<Icon path={mdiPuzzleOutline} size={1} />}
              itemComponent={SelectChallengeItem}
              data={
                challenge?.map((challenge) => ({
                  value: String(challenge.id),
                  label: challenge.title,
                  ...challenge,
                })) ?? []
              }
              filter={(query, challenge) =>
                challenge.title.includes(query) || challenge.value.includes(query)
              }
              nothingFound="没有找到题目"
            />
          </Group>

          <Group position="right">
            <Text fw="bold" size="sm">
              共计 <Code>{instances?.length}</Code> 个已分发容器实例
            </Text>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="xs" w="100%">
        <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 205px)">
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>队伍</th>
                <th>题目</th>
                <th>容器 Id</th>
                <th>生命周期</th>
                <th>访问入口</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {filteredInstances &&
                filteredInstances.map((inst) => {
                  const color = ChallengeTagLabelMap.get(
                    inst.challenge?.tag ?? ChallengeTag.Misc
                  )!.color
                  return (
                    <tr key={inst.containerGuid}>
                      <td>
                        <Box w="100%" h="100%">
                          <Text truncate size="sm" fw="bold" lineClamp={1}>
                            {inst.team?.name}
                          </Text>
                        </Box>
                      </td>
                      <td>
                        <Box w="100%" h="100%">
                          <Text truncate size="sm" fw="bold" lineClamp={1}>
                            {inst.challenge?.title}
                          </Text>
                        </Box>
                      </td>
                      <td>
                        <Text size="sm" ff={theme.fontFamilyMonospace} lineClamp={1}>
                          {inst.containerId?.substring(0, 20)}
                        </Text>
                      </td>
                      <td>
                        <Group noWrap spacing="xs">
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.startedAt).format('MM/DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.expectStopAt).format('MM/DD HH:mm')}
                          </Badge>
                        </Group>
                      </td>
                      <td>
                        <Tooltip
                          label="点击复制"
                          withArrow
                          position="left"
                          classNames={tooltipClasses}
                        >
                          <Text
                            size="sm"
                            color="dimmed"
                            ff={theme.fontFamilyMonospace}
                            style={{
                              backgroundColor: 'transparent',
                              fontSize: theme.fontSizes.sm,
                              cursor: 'pointer',
                            }}
                            onClick={() => {
                              clipBoard.copy(`${inst.publicIP ?? ''}:${inst.publicPort ?? ''}`)
                              showNotification({
                                color: 'teal',
                                message: '实例入口已复制到剪贴板',
                                icon: <Icon path={mdiCheck} size={1} />,
                              })
                            }}
                          >
                            {`${inst.publicIP}:`}
                            <Text span fw="bold" c="white">
                              {inst.publicPort}
                            </Text>
                          </Text>
                        </Tooltip>
                      </td>
                      <td align="right">
                        <Group noWrap spacing="sm" position="right">
                          <ActionIconWithConfirm
                            iconPath={mdiPackageVariantClosedRemove}
                            color="alert"
                            message={`确定销毁容器：\n${inst.containerId} `}
                            disabled={disabled}
                            onClick={() => onDelete(inst.containerGuid)}
                          />
                        </Group>
                      </td>
                    </tr>
                  )
                })}
            </tbody>
          </Table>
        </ScrollArea>
        <Text size="xs" c="dimmed">
          注：容器实例统计不包括管理员测试容器、已销毁的容器
        </Text>
      </Paper>
    </AdminPage>
  )
}

export default Instances
