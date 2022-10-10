import dayjs from 'dayjs'
import React, { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Paper,
  createStyles,
  Table,
  Group,
  Text,
  Avatar,
  Box,
  Stack,
  Pagination,
  Select,
  Tooltip,
  Center,
} from '@mantine/core'
import { Icon } from '@mdi/react'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import api, { ChallengeInfo, ChallengeTag, ScoreboardItem, SubmissionType } from '@Api'
import { BloodsTypes, ChallengeTagLabelMap, SubmissionTypeIconMap } from '../utils/ChallengeItem'
import ScoreboardItemModal from './ScoreboardItemModal'

const useStyles = createStyles((theme) => ({
  table: {
    tableLayout: 'fixed',
    width: 'auto',
    minWidth: '100%',

    '& thead tr th, & tbody tr td': {
      textAlign: 'center',
      padding: '8px',
      whiteSpace: 'nowrap',
      fontSize: 12,
    },
  },
  thead: {
    zIndex: 5,
  },
  theadFixLeft: {
    position: 'sticky',
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
  },
  theadHeader: {
    fontWeight: 'bold',
  },
  theadMono: {
    fontWeight: 'bold',
    fontFamily: theme.fontFamilyMonospace,
  },
  legend: {
    position: 'absolute',
    top: 0,
    left: 0,
    padding: 12,
    float: 'left',
    zIndex: 20,
  },
  noBorder: {
    border: 'none !important',
  },
}))

const Lefts = [0, 55, 95, 265, 335, 390]
const Widths = Array(5).fill(0)
Lefts.forEach((val, idx) => {
  Widths[idx - 1 || 0] = val - Lefts[idx - 1 || 0]
})

const TableHeader = (table: Record<string, ChallengeInfo[]>) => {
  const { classes, cx, theme } = useStyles()

  const hiddenCol = [...Array(5).keys()].map((i) => (
    <th
      key={i}
      className={cx(classes.theadFixLeft, classes.noBorder)}
      style={{ left: Lefts[i], width: Widths[i], minWidth: Widths[i], maxWidth: Widths[i] }}
    >
      &nbsp;
    </th>
  ))

  return (
    <thead className={classes.thead}>
      {/* Challenge Tag */}
      <tr>
        {hiddenCol}
        {Object.keys(table).map((key) => {
          const tag = ChallengeTagLabelMap.get(key as ChallengeTag)!
          return (
            <th
              key={key}
              colSpan={table[key].length}
              style={{
                backgroundColor: theme.fn.rgba(
                  theme.colors[tag.color][theme.colorScheme === 'dark' ? 8 : 6],
                  theme.colorScheme === 'dark' ? 0.15 : 0.2
                ),
              }}
            >
              <Group spacing={4} noWrap position="center" style={{ width: '100%' }}>
                <Icon
                  path={tag.icon}
                  size={1}
                  color={theme.colors[tag.color][theme.colorScheme === 'dark' ? 8 : 6]}
                />
                <Text color={tag.color}>{key}</Text>
              </Group>
            </th>
          )
        })}
      </tr>
      {/* Challenge Name */}
      <tr>
        {hiddenCol}
        {Object.keys(table).map((key) =>
          table[key].map((item) => <th key={item.id}>{item.title}</th>)
        )}
      </tr>
      {/* Headers & Score */}
      <tr>
        {['总排名', '排名', '战队', '解题数量', '总分'].map((header, idx) => (
          <th
            key={idx}
            className={cx(classes.theadFixLeft, classes.theadHeader)}
            style={{ left: Lefts[idx] }}
          >
            {header}
          </th>
        ))}
        {Object.keys(table).map((key) =>
          table[key].map((item) => (
            <th key={item.id} className={classes.theadMono}>
              {item.score}
            </th>
          ))
        )}
      </tr>
    </thead>
  )
}

const TableRow: FC<{
  item: ScoreboardItem
  allRank: boolean
  tableRank: number
  onOpenDetail: () => void
  iconMap: Map<SubmissionType, React.ReactNode>
  challenges?: Record<string, ChallengeInfo[]>
}> = ({ item, challenges, onOpenDetail, iconMap, tableRank, allRank }) => {
  const { classes, cx, theme } = useStyles()
  const { classes: tooltipClasses } = useTooltipStyles()
  const solved = item.challenges?.filter((c) => c.type !== SubmissionType.Unaccepted)
  return (
    <tr>
      <td className={cx(classes.theadMono, classes.theadFixLeft)} style={{ left: Lefts[0] }}>
        {item.rank}
      </td>
      <td className={cx(classes.theadMono, classes.theadFixLeft)} style={{ left: Lefts[1] }}>
        {allRank ? item.rank : item.organizationRank ?? tableRank}
      </td>
      <td className={cx(classes.theadFixLeft)} style={{ left: Lefts[2] }}>
        <Group position="left" spacing={5} noWrap onClick={onOpenDetail}>
          <Avatar
            src={item.avatar}
            radius="xl"
            size={30}
            color="brand"
            sx={(theme) => ({
              ...theme.fn.hover({
                cursor: 'pointer',
              }),
            })}
          >
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Text
            lineClamp={1}
            align="left"
            weight={500}
            sx={(theme) => ({
              userSelect: 'none',

              ...theme.fn.hover({
                cursor: 'pointer',
              }),
            })}
          >
            {item.name}
          </Text>
        </Group>
      </td>
      <td className={cx(classes.theadMono, classes.theadFixLeft)} style={{ left: Lefts[3] }}>
        {solved?.length}
      </td>
      <td className={cx(classes.theadMono, classes.theadFixLeft)} style={{ left: Lefts[4] }}>
        {solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0)}
      </td>
      {challenges &&
        Object.keys(challenges).map((key) =>
          challenges[key].map((item) => {
            const chal = solved?.find((c) => c.id === item.id)
            const icon = iconMap.get(chal?.type ?? SubmissionType.Unaccepted)

            if (!icon) return <td key={item.id} className={classes.theadMono} />

            const tag = ChallengeTagLabelMap.get(item.tag as ChallengeTag)!
            const textStyle = {
              fontSize: '0.9em',
              fontFamily: theme.fontFamilyMonospace,
              fontWeight: 600,
            }

            return (
              <td key={item.id} className={classes.theadMono}>
                <Tooltip
                  classNames={tooltipClasses}
                  transition="pop"
                  label={
                    <Stack align="flex-start" spacing={0} style={{ maxWidth: '20rem' }}>
                      <Text lineClamp={3}>{item.title}</Text>
                      <Text color={tag.color} style={textStyle}>
                        + {chal?.score} pts
                      </Text>
                      <Text color="dimmed" style={textStyle}>
                        # {dayjs(chal?.time).format('MM/DD HH:mm:ss')}
                      </Text>
                    </Stack>
                  }
                >
                  <Center>{icon}</Center>
                </Tooltip>
              </td>
            )
          })
        )}
    </tr>
  )
}

const BloodData = [
  { name: '一血', descr: '+5%' },
  { name: '二血', descr: '+3%' },
  { name: '三血', descr: '+1%' },
]

const ITEM_COUNT_PER_PAGE = 30

interface ScoreboardProps {
  organization: string | null
  setOrganization: (org: string | null) => void
}

const ScoreboardTable: FC<ScoreboardProps> = ({ organization, setOrganization }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { classes } = useStyles()
  const { iconMap } = SubmissionTypeIconMap(1)
  const [activePage, setPage] = useState(1)

  const { data: scoreboard } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const filtered =
    organization === 'all'
      ? scoreboard?.items
      : scoreboard?.items?.filter((s) => s.organization === organization)

  const base = (activePage - 1) * ITEM_COUNT_PER_PAGE
  const currentItems = filtered?.slice(base, base + ITEM_COUNT_PER_PAGE)

  const [currentItem, setCurrentItem] = useState<ScoreboardItem | null>(null)
  const [itemDetailOpened, setItemDetailOpened] = useState(false)

  return (
    <Paper shadow="md" p="md">
      <Stack spacing="xs">
        {scoreboard?.timeLines && Object.keys(scoreboard.timeLines).length > 1 && (
          <Group>
            <Select
              defaultValue="all"
              data={[
                { value: 'all', label: '总排行' },
                ...Object.keys(scoreboard.timeLines)
                  .filter((k) => k !== 'all')
                  .map((o) => ({
                    value: o,
                    label: o === 'all' ? '总排行' : o,
                  })),
              ]}
              value={organization}
              onChange={(org) => {
                setOrganization(org)
                setPage(1)
              }}
              styles={{
                input: {
                  width: 300,
                },
              }}
            />
          </Group>
        )}
        <Box style={{ position: 'relative' }}>
          <Box
            sx={{
              maxWidth: '100%',
              overflow: 'scroll',
              '::-webkit-scrollbar': {
                height: 0,
              },
            }}
          >
            <Table className={classes.table}>
              <TableHeader {...scoreboard?.challenges} />
              <tbody>
                {scoreboard &&
                  currentItems?.map((item, idx) => (
                    <TableRow
                      key={base + idx}
                      allRank={organization === 'all'}
                      tableRank={base + idx + 1}
                      item={item}
                      onOpenDetail={() => {
                        setCurrentItem(item)
                        setItemDetailOpened(true)
                      }}
                      challenges={scoreboard.challenges}
                      iconMap={iconMap}
                    />
                  ))}
              </tbody>
            </Table>
          </Box>
          <Box className={classes.legend}>
            <Stack spacing="xs">
              <Group spacing="lg">
                {BloodsTypes.map((type, idx) => (
                  <Group key={idx} position="left" spacing={2}>
                    {iconMap.get(type)}
                    <Text size="sm">{BloodData[idx].name}</Text>
                    <Text size="xs" color="dimmed">
                      {BloodData[idx].descr}
                    </Text>
                  </Group>
                ))}
              </Group>
              <Text size="sm" color="dimmed">
                注：同分队伍以得分时间先后排名
              </Text>
            </Stack>
          </Box>
        </Box>
        <Group position="apart">
          <Text size="sm" color="dimmed">
            Tip: 可以按左右方向键滚动题目列表哦~
          </Text>
          <Pagination
            page={activePage}
            onChange={setPage}
            total={Math.ceil((filtered?.length ?? 1) / ITEM_COUNT_PER_PAGE)}
            boundaries={2}
          />
        </Group>
      </Stack>
      <ScoreboardItemModal
        challenges={scoreboard?.challenges}
        opened={itemDetailOpened}
        centered
        withCloseButton={false}
        size="40rem"
        onClose={() => setItemDetailOpened(false)}
        item={currentItem}
      />
    </Paper>
  )
}

export default ScoreboardTable
