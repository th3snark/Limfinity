# This script is called by the PcrPlateTool using the API.
data = params[:data]

plate_name = data[:plate_name]

results = find_subjects(query: search_query(subject_type: 'Plate'){|qb|
  qb.and(
    qb.prop("name").eq(plate_name)
)}, limit:1)

plate = results[0]
samples = plate['Samples']

if(plate['384'])
  columnCount = 24
else
  columnCount = 12
end

a = []

samples.each do |s|
  st = s['Sample Type']
  rs = s['Root Sample']

  idx = s['Index'].to_i
  r_idx = idx / columnCount;
  c_idx = idx % columnCount;
  r = (r_idx + 65).chr
  c = c_idx + 1

  a << ({
       :index => idx,
       :position => "#{r}#{c}",
       :name => s.name,
       :terminated => s.terminated?,
       :canceled => s['Canceled'],
       :root_sample => rs.name,
       :sample_type => st
       })
end

a.sort_by! { |x| x[:index] }

{
  :data => data,
  :result =>
  ({
    :plate =>
    ({
      :name => plate.name,
      :plate_type => plate['Plate Type'],
      :terminated => plate.terminated?,
      :canceled => plate['Canceled'],
      :sample_count => samples.count,
      :column_count => columnCount,
    }),
    :samples => a
  })
}